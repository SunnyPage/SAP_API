﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase {
        public class ClientSearchDetail : SearchDetail {
            public string CardCode { get; set; }
            public string CardFName { get; set; }
            public string CardName { get; set; }
        }

        public class ClientSearchResponse : SearchResponse<ClientSearchDetail> {}

        public class ProviderSearchDetail : SearchDetail {
            public string CardCode { get; set; }
            public string CardFName { get; set; }
            public string CardName { get; set; }
            public string Currency { get; set; }
        }

        public class ProviderSearchResponse : SearchResponse<ProviderSearchDetail> { }

        [HttpPost("clients/search")]
        public async Task<IActionResult> Get([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0) {
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~Remove 2nd DB

            //1 DB Config
            //SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~1 DB Config

            List<string> where = new List<string>();

            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(\"CardCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(\"CardFName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(\"CardName\") Like LOWER('%{request.columns[2].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY \"CardCode\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY \"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY \"CardName\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY \"CardCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select ""CardCode"", ""CardName"", ""CardFName""
                From OCRD Where ""CardType"" = 'C' AND ""CardCode"" NOT LIKE '%-D'";

            if (where.Count != 0) {
                query += " AND " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"].ToObject<List<ClientSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                 From OCRD Where ""CardType"" = 'C' AND ""CardCode"" NOT LIKE '%-D' ";

            if (where.Count != 0) {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0]["COUNT"].ToObject<int>();

            var respose = new ClientSearchResponse
            {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(respose);
        }

        [HttpPost("providers/search")]
        public async Task<IActionResult> GetProviders([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0) {
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~Remove 2nd DB

            //1 DB Config
            //SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~1 DB Config

            List<string> where = new List<string>();

            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(\"CardCode\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(\"CardName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(\"CardFName\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty) {
                where.Add($"LOWER(\"Currency\") Like LOWER('%{request.columns[3].search.value}%')");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY \"CardCode\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY \"CardName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY \"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 3) {
                orderby = $" ORDER BY \"Currency\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY \"CardCode\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select ""CardCode"", ""CardName"", ""CardFName"", ""Currency""
                From OCRD Where ""CardType"" = 'S' ";

            if (where.Count != 0) {
                query += " AND " + whereClause;
            }

            query += orderby;

            query += " LIMIT " + request.length + " OFFSET " + request.start + "";

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"].ToObject<List<ProviderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                 From OCRD Where ""CardType"" = 'S' ";

            if (where.Count != 0) {
                queryCount += " AND " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0]["COUNT"].ToObject<int>();

            var respose = new ProviderSearchResponse
            {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(respose);
        }

        [HttpGet("CRMClientToSell/{CardCode}")]
        public async Task<IActionResult> GetCRMClientToSell(string CardCode) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0) {
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~Remove 2nd DB
            
            //1 DB Config
            //SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~1 DB Config

            JToken contact;

            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    contact.""ListNum"",
                    paymentTerm.""GroupNum"",
                    paymentTerm.""PymntGroup"",
                    paymentMethod.""PayMethCod"",
                    paymentMethod.""Descript"",
                    ""SlpName"",
                    ""ListName""
                From OCRD contact
                JOIN OSLP seller ON contact.""SlpCode"" = seller.""SlpCode""
                JOIN OCTG paymentTerm ON paymentTerm.""GroupNum"" = contact.""GroupNum""
                JOIN OPLN priceList ON priceList.""ListNum"" = contact.""ListNum""
                LEFT JOIN OPYM paymentMethod ON paymentMethod.""PayMethCod"" = contact.""PymCode""
                Where ""CardCode"" = '" + CardCode + "'");
            oRecSet.MoveFirst();
            if (oRecSet.RecordCount == 0) {
                return NotFound("No Existe Contacto");
            }
            contact = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0];
            oRecSet.DoQuery(@"
                Select
                    paymentMethodCardCode.""PymCode"",
                    paymentMethod.""Descript""
                From CRD2 paymentMethodCardCode
                JOIN OPYM paymentMethod ON paymentMethod.""PayMethCod"" = paymentMethodCardCode.""PymCode""
                Where ""CardCode"" = '" + CardCode  + "'");
            contact["PaymentMethods"] = context.XMLTOJSON(oRecSet.GetAsXML())["CRD2"];
            ContactToSell contactToSell = contact.ToObject<ContactToSell>();
            oRecSet = null;
            contact = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(contactToSell);
        }

        [HttpGet("CRMProviderToBuy/{id}")]
        public async Task<IActionResult> CRMProviderToBuy(string id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0) {
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~Remove 2nd DB

            //1 DB Config
            //SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~1 DB Config

            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    ""Currency""
                From OCRD
                Where ""CardCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            if (oRecSet.RecordCount == 0) {
                return NotFound("No Existe Contacto");
            }

            JToken contact = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"][0];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(contact);
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [HttpGet("CRM/{id}")]
        public async Task<IActionResult> GetCRMID(string id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0) {
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.BusinessPartners items = (SAPbobsCOM.BusinessPartners)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);
            SAPbobsCOM.SalesPersons seller = (SAPbobsCOM.SalesPersons)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oSalesPersons);
            SAPbobsCOM.PaymentTermsTypes payment = (SAPbobsCOM.PaymentTermsTypes)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPaymentTermsTypes);
            SAPbobsCOM.UserTable sboTable = (SAPbobsCOM.UserTable)context.oCompany2.UserTables.Item("SO1_01FORMAPAGO");
            //~Remove 2nd DB

            //1 DB Config
            //SAPbobsCOM.BusinessPartners items = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);
            //SAPbobsCOM.SalesPersons seller = (SAPbobsCOM.SalesPersons)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oSalesPersons);
            //SAPbobsCOM.PaymentTermsTypes payment = (SAPbobsCOM.PaymentTermsTypes)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPaymentTermsTypes);
            //SAPbobsCOM.UserTable sboTable = (SAPbobsCOM.UserTable)context.oCompany.UserTables.Item("SO1_01FORMAPAGO");
            //~1 DB Config

            JToken pagos = context.XMLTOJSON(sboTable.GetAsXML())["OCRD"];

            if (items.GetByKey(id)) {

                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["OCRD"] = temp["OCRD"][0];

                if (seller.GetByKey(temp["OCRD"]["SlpCode"].ToObject<int>())) {
                    JToken temp2 = context.XMLTOJSON(seller.GetAsXML());
                    temp["OSLP"] = temp2["OSLP"][0];
                }
                if (payment.GetByKey(temp["OCRD"]["GroupNum"].ToObject<int>())) {
                    JToken temp3 = context.XMLTOJSON(payment.GetAsXML());
                    temp["OCTG"] = temp3["OCTG"][0];
                }

                temp["PAGO"] = pagos;
                return Ok(temp);
            }
            return NotFound("No Existe Contacto");
        }

        // GET: api/Contact/CRMList
        [HttpGet("CRMList")]
        public async Task<IActionResult> GetCRMList() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"CardCode\", \"CardName\", \"CardFName\"  From OCRD Where \"CardType\" = 'C' AND \"CardCode\" NOT LIKE '%-D'");
            oRecSet.MoveFirst();
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //return Ok(context.comp(contacts, 0).Result);
            return Ok(contacts);
        }

        // GET: api/Contact/CRMListProveedor
        [HttpGet("CRMListProveedor")]
        public async Task<IActionResult> GetCRMListProveedor() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery("Select \"CardCode\", \"CardName\", \"Currency\", \"CardFName\"  From OCRD Where \"CardType\" = 'S'");
            oRecSet.MoveFirst();
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(contacts);
        }

        // GET: api/Contact/APPCRM/200
        [HttpGet("APPCRM/{id}")]
        public async Task<IActionResult> GetAPPCRM(int id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0){
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~Remove 2nd DB

            //1 DB Config
            //SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~1 DB Config

            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    ""Address"",
                    ""ZipCode"",
                    ""Country"",
                    ""Block"",
                    ""GroupNum"",
                    ""ListNum""
                From OCRD employeeSales
                JOIN OHEM employee ON ""SlpCode"" = ""salesPrson""
                Where ""CardType"" = 'C' AND ""empID"" = " + id + @" AND ""CardCode"" NOT LIKE '%-D'");
            if (oRecSet.RecordCount == 0) {
                return Ok(new List<string>());
            }
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //return Ok(await context.comp(contacts, 3));
            return Ok(contacts);
        }

        // GET: api/Contact/APPCRM
        [HttpGet("APPCRM")]
        public async Task<IActionResult> GetAPPCRMs() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            //Remove 2nd DB
            if (!context.oCompany2.Connected) {
                int code = context.oCompany2.Connect();
                if (code != 0) {
                    string error = context.oCompany2.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
            }
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany2.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~Remove 2nd DB

            //1 DB Config
            //SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            //~1 DB Config

            oRecSet.DoQuery(@"
                Select
                    ""CardCode"",
                    ""CardName"",
                    ""CardFName"",
                    ""Address"",
                    ""ZipCode"",
                    ""Country"",
                    ""Block"",
                    ""GroupNum"",
                    ""ListNum""
                From OCRD Where ""CardType"" = 'C' AND ""CardCode"" NOT LIKE '%-D'");
            oRecSet.MoveFirst();
            JToken contacts = context.XMLTOJSON(oRecSet.GetAsXML())["OCRD"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //return Ok(await context.comp(contacts, 3));
            return Ok(contacts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.BusinessPartners items = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);

            if (items.GetByKey(id)) {
                JToken contact = context.XMLTOJSON(items.GetAsXML());
                return Ok(contact);
            }
            return NotFound("No Existe Contacto");
        }

    }
}
