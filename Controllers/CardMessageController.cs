using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MsgGenerator.Models;
using System.Data.Entity.Infrastructure;

namespace MsgGenerator.Controllers
{
    public class CardMessageController : Controller
    {
        private CardsDBContext db = new CardsDBContext();

        // GET: /CardMessage/
        public ActionResult Index(string FilterBy = "Activation")
        {
            var cardmessages = db.CardMessages.Where(cm => cm.MsgSubject == FilterBy);
            return View(cardmessages.ToList());
        }

        // GET: /CardMessage/Create
        public ActionResult Create()
        {
            PopulateFilterDropDownList(); 
            return View();
        }

        // POST: /CardMessage/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include="MsgSubject,CardType,MsgText")] CardMessage cardmessage)
        {
            if (ModelState.IsValid)
            {
                db.CardMessages.Add(cardmessage);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            PopulateFilterDropDownList(cardmessage.id);
            return View(cardmessage);
        }

        // GET: /CardMessage/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CardMessage cardmessage = db.CardMessages.Find(id);
            if (cardmessage == null)
            {
                return HttpNotFound();
            }
            PopulateFilterDropDownList(cardmessage.MsgSubject);
            return View(cardmessage);
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult EditPost(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var cardmessageToUpdate = db.CardMessages.Find(id);
            if (TryUpdateModel(cardmessageToUpdate, "", new string[] { "MsgSubject", "CardType", "MsgText" }))
            {
                try
                {
                    db.Entry(cardmessageToUpdate).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again later or contact administrator if error persist");
                }
            }
            PopulateFilterDropDownList(cardmessageToUpdate.MsgSubject);
            return View(cardmessageToUpdate);
        }

        private void PopulateFilterDropDownList(object selectedSubject = null)
        {
            var subjectsQuery = (from cm in db.CardMessages
                                 select new { Mtext = cm.MsgSubject, Mvalue = cm.MsgSubject }).Distinct();
            ViewBag.MsgSubject = new SelectList(subjectsQuery, "Mtext", "Mvalue");
        }


        // GET: /CardMessage/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CardMessage cardmessage = db.CardMessages.Find(id);
            if (cardmessage == null)
            {
                return HttpNotFound();
            }
            return View(cardmessage);
        }

        // POST: /CardMessage/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            CardMessage cardmessage = db.CardMessages.Find(id);
            db.CardMessages.Remove(cardmessage);
            db.SaveChanges();
            return RedirectToAction("Index");
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
