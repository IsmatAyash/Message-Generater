using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using MsgGenerator.Models;
using System.Data.SqlClient;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MsgGenerator.Controllers
{
    public class ImportExportDataController : Controller
    {
        private CardsDBContext db = new CardsDBContext();
        private string connectionString = "data source=ROUWSRV;initial catalog=master;integrated security=SSPI;";

        // GET: /ImportExportData/
        public async Task<ActionResult> Index(string FilterBy)
        {
            var subjectsQuery = (from cm in db.CardMessages
                                 select new { Mtext = cm.MsgSubject, Mvalue = cm.MsgSubject }).Distinct();
            ViewBag.MsgSubject = new SelectList(subjectsQuery, "Mtext", "Mvalue");

            var msgrecipients = await db.MsgRecipients.ToListAsync();
            if ( !String.IsNullOrEmpty(FilterBy) && FilterBy != "All")
            {
                msgrecipients = msgrecipients.Where(cm => cm.MsgSubject == FilterBy).ToList();
                ViewBag.RecordCount = "Total Count = " + msgrecipients.Count;

                //Get last imported date
                ViewBag.LastImported = ", Last Imported = " + (msgrecipients.Count != 0 ? msgrecipients.OrderByDescending(mr => mr.LastImported).Take(1).Single().LastImported.ToString() : "");
                return View(msgrecipients);
            }
            else
            {
                ViewBag.RecordCount = "Total Count = " + msgrecipients.Count;
                ViewBag.LastImported = ", Last Imported = " + (msgrecipients.Count != 0 ? msgrecipients.OrderByDescending(mr => mr.LastImported).Take(1).Single().LastImported.ToString() : "");
                return View(msgrecipients.Take(0));
            }
        }

        #region ExecutePackage
        public ActionResult ImportData()
        {
            Dictionary<string, SqlParameter> procParameters = new Dictionary<string, SqlParameter>();
            SqlParameter output_execution_id = new SqlParameter
            {
                ParameterName = "output_execution_id",
                Direction = ParameterDirection.Output,
                DbType = DbType.Int64
            };
            procParameters.Add("output_execution_id", output_execution_id);

            int rc = ExecSqlProc("dbo.execute_ssis_CardSMSMessages", procParameters);

            //querying the status of the package after running. The possible values are created(1), running(2), canceled(3), failed(4), pending(5), ended unexpectedly (6), succeeded(7), stopping(8), and completed(9).
            string sqlQuery = "select status from SSISDB.catalog.executions where execution_id = " + procParameters["output_execution_id"].Value.ToString();
            SqlDataReader rdr = ExecSqlQuery(sqlQuery);
            if (rdr.Read() == true)
            {
                // 7 = Succeeded, 9 = completed
                if (rdr[0].ToString() == "7" || rdr[0].ToString() == "9")
                {
                    TempData["Message"] = "  Import process has completed successfully";
                }
                else
                {
                    TempData["Message"] = "  Import process has failed ";
                }
            }
            return RedirectToAction("Index");
        }

        public int ExecSqlProc(string storedProcName, Dictionary<string, SqlParameter> procParameters)
        {
            int rc;
            SqlConnection cn = new SqlConnection(connectionString);

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cn;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = storedProcName;

            foreach (var procParameter in procParameters)
            {
                cmd.Parameters.Add(procParameter.Value);
            }
            cn.Open();
            rc = cmd.ExecuteNonQuery();

            foreach (var procParameter in procParameters)
            {
                SqlParameter outputParameter = procParameter.Value;
                if (outputParameter.Direction == ParameterDirection.Output)
                {
                    outputParameter.Value = cmd.Parameters[outputParameter.ParameterName].Value;
                }
            }
            return rc;
        }

        public SqlDataReader ExecSqlQuery(string sqlQuery)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cn;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sqlQuery;
            cn.Open();

            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        #endregion

        public ActionResult ExportToExcel(string ExportWhat, string ExportTo)
        {
            int totalExported = 0;
            if (ExportTo == "Excel")
            {
                GridView gv = new GridView();
                var rowstoexport = PrepareExportData(ExportWhat);
                gv.DataSource = rowstoexport;
                gv.DataBind();
                Response.ClearContent();
                Response.Buffer = true;
                Response.AddHeader("content-disposition", "attachment; filename=CardMessages.xls");
                Response.ContentType = "application/ms-excel";
                Response.Charset = "";
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);
                Response.Output.Write(sw.ToString());
                Response.Flush();
                Response.End();
                totalExported = rowstoexport.Count;
            }
            else
            {
                ExportToCSV(out totalExported,ExportWhat);
            }
            TempData["TotalExported"] = totalExported;
            return RedirectToAction("Index");
        }

        private void ExportToCSV(out int totalExported, string ExportWhat)
        {

            StringWriter sw = new StringWriter();

            sw.WriteLine("Subject,Mobile,Name,Body");

            Response.ClearContent();
            Response.AddHeader("content-disposition", "attachment;filename=CardMessages.txt");
            Response.ContentType = "text/csv";

            var rowstoexport = PrepareExportData(ExportWhat);

            foreach (var line in rowstoexport)
            {
                sw.WriteLine(string.Format("{0},{1},{2},{3}",
                                           line.MsgSubject,
                                           line.Mobile,
                                           line.Name,
                                           line.MsgText));
            }

            Response.Write(sw.ToString());
            Response.End();
            totalExported = rowstoexport.Count;
        }

        private List<MsgRecipient> PrepareExportData(string ExportWhat)
        {
            var msgrecipients = db.MsgRecipients.ToList();
            switch (ExportWhat)
            {
                case "LatePayments":
                    msgrecipients = msgrecipients.Where(mr => new[] { "NI", "CSC" }.Contains(mr.MsgSubject)).ToList();
                    break;
                case "CardMsgs":
                    msgrecipients = msgrecipients.Where(mr => new[] { "Activation", "Issuance", "Renewal", "Fraud" }.Contains(mr.MsgSubject)).ToList();
                    break;
                default:
                    msgrecipients = msgrecipients.Where(mr => mr.MsgSubject == ExportWhat).ToList();
                    break;
            }
            return msgrecipients;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
