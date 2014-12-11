using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using PanoramicDataModel;

namespace PanoramicDataService.Apis
{
    public class MetaTableInfoController : ApiController
    {
        private panoramicdataEntities db = new panoramicdataEntities();

        // GET api/MetaTableInfo
        public IEnumerable<TableInfo> GetTableInfoes()
        {
            return db.TableInfoes.AsEnumerable();
        }

        // GET api/MetaTableInfo/5
        public TableInfo GetTableInfo(int id)
        {
            TableInfo tableinfo = db.TableInfoes.Find(id);
            if (tableinfo == null)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            }

            return tableinfo;
        }

        // PUT api/MetaTableInfo/5
        public HttpResponseMessage PutTableInfo(int id, TableInfo tableinfo)
        {
            if (ModelState.IsValid && id == tableinfo.Id)
            {
                db.Entry(tableinfo).State = EntityState.Modified;

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        // POST api/MetaTableInfo
        public HttpResponseMessage PostTableInfo(TableInfo tableinfo)
        {
            if (ModelState.IsValid)
            {
                db.TableInfoes.Add(tableinfo);
                db.SaveChanges();

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.Created, tableinfo);
                response.Headers.Location = new Uri(Url.Link("DefaultApi", new { id = tableinfo.Id }));
                return response;
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        // DELETE api/MetaTableInfo/5
        public HttpResponseMessage DeleteTableInfo(int id)
        {
            TableInfo tableinfo = db.TableInfoes.Find(id);
            if (tableinfo == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            db.TableInfoes.Remove(tableinfo);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            return Request.CreateResponse(HttpStatusCode.OK, tableinfo);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}