using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace MsgGenerator.Controllers
{
    public class GetDataController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Getting data....";
            SqlConnection jobConnection;
            SqlCommand jobCommand;
            SqlParameter jobReturnValue;
            SqlParameter jobParameter;
            int jobResult;

            jobConnection = new SqlConnection("Data Source=RETEBREPORTS;Initial Catalog=msdb;Integrated Security=SSPI");
            jobCommand = new SqlCommand("sp_start_job", jobConnection);
            jobCommand.CommandType = CommandType.StoredProcedure;

            jobReturnValue = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
            jobReturnValue.Direction = ParameterDirection.ReturnValue;
            jobCommand.Parameters.Add(jobReturnValue);

            jobParameter = new SqlParameter("@job_name", SqlDbType.VarChar);
            jobParameter.Direction = ParameterDirection.Input;
            jobCommand.Parameters.Add(jobParameter);
            jobParameter.Value = "ExecuteCardMessageSSIS";

            jobConnection.Open();
            jobCommand.ExecuteNonQuery();
            jobResult = (Int32)jobCommand.Parameters["@RETURN_VALUE"].Value;
            jobConnection.Close();

            switch (jobResult)
            {
                case 0:
                    ViewBag.Message = "SQL Server Agent job, RunSISSPackage, started successfully.";
                    break;
                default:
                    ViewBag.Message = "SQL Server Agent job, RunSISSPackage, failed to start.";
                    break;
            }
            return View();
        }
    }
}