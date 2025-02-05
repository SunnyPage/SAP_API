﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Entities;
using SAP_API.Models;

namespace SAP_API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase {

        /// <summary>
        /// Get Order List to CRM web Filter by DatatableParameters.
        /// </summary>
        /// <param name="request">DataTableParameters</param>
        /// <returns>OrderSearchResponse</returns>
        /// <response code="200">OrderSearchResponse(SearchResponse)</response>
        // POST: api/Order/Search
        [ProducesResponseType(typeof(OrderSearchResponse), StatusCodes.Status200OK)]
        [HttpPost("Search")] 
        public async Task<IActionResult> Search([FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();
            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(ord.\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(employee.\"SlpName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(contact.\"CardFName\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty) {
                where.Add($"LOWER(contact.\"CardName\") Like LOWER('%{request.columns[3].search.value}%')");
            }
            if (request.columns[4].search.value != String.Empty) {
                where.Add($"LOWER(warehouse.\"WhsName\") Like LOWER('%{request.columns[4].search.value}%')");
            }
            if (request.columns[5].search.value != String.Empty) {
                where.Add($"ord.\"DocTotal\" = {request.columns[5].search.value}");
            }
            if (request.columns[6].search.value != String.Empty) {
                where.Add($"LOWER(ord.\"DocCur\") Like LOWER('%{request.columns[6].search.value}%')");
            }
            if (request.columns[7].search.value != String.Empty) {
                where.Add($"LOWER(payment.\"PymntGroup\") Like LOWER('%{request.columns[7].search.value}%')");
            }
            if (request.columns[8].search.value != String.Empty) {

                List<string> whereOR = new List<string>();
                if ("Abierto".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"ord.""DocStatus"" = 'O' ");
                }
                if ("Cerrado".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"ord.""DocStatus"" = 'C' ");
                }
                if ("Cancelado".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"ord.""CANCELED"" = 'Y' ");
                }

                string whereORClause = "(" + String.Join(" OR ", whereOR) + ")";
                where.Add(whereORClause);
            }
            if (request.columns[9].search.value != String.Empty) {
                where.Add($"to_char(to_date(SUBSTRING(ord.\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[9].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY ord.\"DocNum\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY employee.\"SlpName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY contact.\"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 3) {
                orderby = $" ORDER BY contact.\"CardName\" {request.order[0].dir}";
            } else if (request.order[0].column == 4) {
                orderby = $" ORDER BY warehouse.\"WhsName\" {request.order[0].dir}";
            } else if (request.order[0].column == 5) {
                orderby = $" ORDER BY ord.\"DocTotal\" {request.order[0].dir}";
            } else if (request.order[0].column == 6) {
                orderby = $" ORDER BY ord.\"DocCur\" {request.order[0].dir}";
            } else if (request.order[0].column == 7) {
                orderby = $" ORDER BY payment.\"PymntGroup\" {request.order[0].dir}";
            } else if (request.order[0].column == 8) {
                orderby = $" ORDER BY ord.\"DocStatus\" {request.order[0].dir}";
            } else if (request.order[0].column == 9) {
                orderby = $" ORDER BY ord.\"DocDate\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY ord.\"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",

                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",

                    (case when ord.""DocCur"" = 'USD' then ord.""DocTotalFC""
                    else ord.""DocTotal"" end)  AS  ""DocTotal"",

                    ord.""CardName"",
                    ord.""DocCur"",
                    payment.""PymntGroup"",
                    contact.""CardFName"",
                    employee.""SlpName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OSLP employee ON ord.""SlpCode"" = employee.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                query += "Where " + whereClause;
            }

            query += orderby;

            if (request.length != -1) {
                query += " LIMIT " + request.length + " OFFSET " + request.start + "";
            }

            oRecSet.DoQuery(query);
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"].ToObject<List<OrderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OSLP employee ON ord.""SlpCode"" = employee.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                queryCount += "Where " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0]["COUNT"].ToObject<int>();

            var respose = new OrderSearchResponse {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            return Ok(respose);
        }

        // TODO: this route is temporary.
        // After the database integration, the user identification token must 
        // be used to know whether to use filtering by Warehouse or not. 
        // This has to be done in route "Search"
        /// <summary>
        /// Get Order List to CRM web Filter by DatatableParameters and Warehouse.
        /// </summary>
        /// <param name="WhsCode">Warehouse Code</param>
        /// <param name="request">DataTableParameters</param>
        /// <returns>OrderSearchResponse</returns>
        /// <response code="200">OrderSearchResponse(SearchResponse)</response>
        // POST: api/Order/Search/:WhsCode
        [ProducesResponseType(typeof(OrderSearchResponse), StatusCodes.Status200OK)]
        [HttpPost("Search/{WhsCode}")]
        public async Task<IActionResult> SearchWarehouseFilter(string WhsCode, [FromBody] SearchRequest request) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<string> where = new List<string>();
            where.Add($"warehouse.\"WhsCode\" = '{WhsCode}'");

            if (request.columns[0].search.value != String.Empty) {
                where.Add($"LOWER(ord.\"DocNum\") Like LOWER('%{request.columns[0].search.value}%')");
            }
            if (request.columns[1].search.value != String.Empty) {
                where.Add($"LOWER(employee.\"SlpName\") Like LOWER('%{request.columns[1].search.value}%')");
            }
            if (request.columns[2].search.value != String.Empty) {
                where.Add($"LOWER(contact.\"CardFName\") Like LOWER('%{request.columns[2].search.value}%')");
            }
            if (request.columns[3].search.value != String.Empty) {
                where.Add($"LOWER(contact.\"CardName\") Like LOWER('%{request.columns[3].search.value}%')");
            }
            if (request.columns[5].search.value != String.Empty) {
                where.Add($"ord.\"DocTotal\" = {request.columns[5].search.value}");
            }
            if (request.columns[6].search.value != String.Empty) {
                where.Add($"LOWER(ord.\"DocCur\") Like LOWER('%{request.columns[6].search.value}%')");
            }
            if (request.columns[7].search.value != String.Empty) {
                where.Add($"LOWER(payment.\"PymntGroup\") Like LOWER('%{request.columns[7].search.value}%')");
            }
            if (request.columns[8].search.value != String.Empty) {

                List<string> whereOR = new List<string>();
                if ("Abierto".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"ord.""DocStatus"" = 'O' ");
                }
                if ("Cerrado".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"ord.""DocStatus"" = 'C' ");
                }
                if ("Cancelado".Contains(request.columns[8].search.value, StringComparison.CurrentCultureIgnoreCase)) {
                    whereOR.Add(@"ord.""CANCELED"" = 'Y' ");
                }

                string whereORClause = "(" + String.Join(" OR ", whereOR) + ")";
                where.Add(whereORClause);
            }
            if (request.columns[9].search.value != String.Empty) {
                where.Add($"to_char(to_date(SUBSTRING(ord.\"DocDate\", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') Like '%{request.columns[9].search.value}%'");
            }

            string orderby = "";
            if (request.order[0].column == 0) {
                orderby = $" ORDER BY ord.\"DocNum\" {request.order[0].dir}";
            } else if (request.order[0].column == 1) {
                orderby = $" ORDER BY employee.\"SlpName\" {request.order[0].dir}";
            } else if (request.order[0].column == 2) {
                orderby = $" ORDER BY contact.\"CardFName\" {request.order[0].dir}";
            } else if (request.order[0].column == 3) {
                orderby = $" ORDER BY contact.\"CardName\" {request.order[0].dir}";
            } else if (request.order[0].column == 5) {
                orderby = $" ORDER BY ord.\"DocTotal\" {request.order[0].dir}";
            } else if (request.order[0].column == 6) {
                orderby = $" ORDER BY ord.\"DocCur\" {request.order[0].dir}";
            } else if (request.order[0].column == 7) {
                orderby = $" ORDER BY payment.\"PymntGroup\" {request.order[0].dir}";
            } else if (request.order[0].column == 8) {
                orderby = $" ORDER BY ord.\"DocStatus\" {request.order[0].dir}";
            } else if (request.order[0].column == 9) {
                orderby = $" ORDER BY ord.\"DocDate\" {request.order[0].dir}";
            } else {
                orderby = $" ORDER BY ord.\"DocNum\" DESC";
            }

            string whereClause = String.Join(" AND ", where);

            string query = @"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",

                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",
                    
                    (case when ord.""DocCur"" = 'USD' then ord.""DocTotalFC""
                    else ord.""DocTotal"" end)  AS  ""DocTotal"",

                    ord.""CardName"",
                    ord.""DocCur"",
                    payment.""PymntGroup"",
                    contact.""CardFName"",
                    employee.""SlpName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OSLP employee ON ord.""SlpCode"" = employee.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                query += "Where " + whereClause;
            }

            query += orderby;

            if (request.length != -1) {
                query += " LIMIT " + request.length + " OFFSET " + request.start + "";
            }

            oRecSet.DoQuery(query);
            oRecSet.MoveFirst();
            var orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"].ToObject<List<OrderSearchDetail>>();

            string queryCount = @"
                Select
                    Count (*) as COUNT
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OSLP employee ON ord.""SlpCode"" = employee.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode"" ";

            if (where.Count != 0) {
                queryCount += "Where " + whereClause;
            }
            oRecSet.DoQuery(queryCount);
            oRecSet.MoveFirst();
            int COUNT = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0]["COUNT"].ToObject<int>();

            var respose = new OrderSearchResponse {
                data = orders,
                draw = request.Draw,
                recordsFiltered = COUNT,
                recordsTotal = COUNT,
            };
            return Ok(respose);
        }

        // GET: api/Order/WMSDetail/5
        [HttpGet("WMSDetail/{DocEntry}")]
        public async Task<IActionResult> GetWMSDetail(int DocEntry) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            OrderDetail orderDetail;
            JToken order;
            string DocCur;
            oRecSet.DoQuery(@"
                SELECT
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    to_char(to_date(SUBSTRING(ord.""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",
                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(ord.""CancelDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""CancelDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",

                    (case when ord.""DocCur"" = 'USD' then ord.""DocTotalFC""
                    else ord.""DocTotal"" end)  AS  ""Total"",

                    SUBSTRING(ord.""DocTime"" , 0, LENGTH(ord.""DocTime"")-2) || ':' || RIGHT(ord.""DocTime"",2) as ""DocTime"",
                    
                    ord.""Address"",
                    ord.""Address2"",
                    ord.""DocCur"",
                    ord.""Comments"",
                    ord.""DocRate"",
                    payment.""PymntGroup"",
                    contact.""CardName"",
                    contact.""CardCode"",
                    contact.""CardFName"",
                    contact.""ListNum"",
                    employee.""SlpCode"",
                    employee.""SlpName"",
                    warehouse.""WhsCode"",
                    warehouse.""WhsName""
                FROM ORDR ord
                LEFT JOIN NNM1 series ON series.""Series"" = ord.""Series""
                LEFT JOIN OWHS warehouse ON warehouse.""WhsCode"" = series.""SeriesName""
                LEFT JOIN OSLP employee ON employee.""SlpCode"" = ord.""SlpCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OCRD contact ON contact.""CardCode"" = ord.""CardCode""
                WHERE ord.""DocEntry"" = '" + DocEntry + "' ");

            if (oRecSet.RecordCount == 0) {
                return NotFound("No Existe Documento");
            }

            order = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0];
            DocCur = order["DocCur"].ToString();
            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""Dscription"",
                    ""Price"",
                    ""Currency"",

                    (case when ""U_CjsPsVr"" != '0' then ""U_CjsPsVr""
                    else ""Quantity"" end)  AS  ""Quantity"",
                    
                    (case when ""U_CjsPsVr"" != '0' then 'CAJA'
                    else ""UomCode"" end)  AS  ""UomCode"",
                    
                    ""InvQty"",
                    ""UomCode2"",

                    (case when '" + DocCur + @"' = 'USD' then ""TotalFrgn""
                    else ""LineTotal"" end)  AS  ""Total""

                From RDR1 Where ""DocEntry"" = '" + DocEntry + "'");
            oRecSet.MoveFirst();
            order["OrderRows"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            orderDetail = order.ToObject<OrderDetail>();

            order = null;
            oRecSet = null;
            DocCur = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orderDetail);
        }

        /*
        // GET: api/Order/WMSDetail/5
        [HttpGet("Test/{DocEntry}")]
        public async Task<IActionResult> GetTest(int DocEntry) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);

            if (order.GetByKey(DocEntry)) {
                order.Lines.SetCurrentLine(2);
                Console.WriteLine(order.Lines.Quantity);
                Console.WriteLine(order.Lines.RemainingOpenQuantity);
                Console.WriteLine(order.Lines.LineStatus);

                order.Lines.Quantity -= order.Lines.RemainingOpenQuantity;

                int result = order.Update();
                if (result == 0) {
                    order.GetByKey(DocEntry);
                    Console.WriteLine(order.Lines.Quantity);
                    Console.WriteLine(order.Lines.RemainingOpenQuantity);
                    Console.WriteLine(order.Lines.LineStatus);
                    return Ok();
                }
                else {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }
                return Ok();

            }

            return BadRequest(new { error = "No Existe Documento" });

        }
        */

        // GET: api/Order/5
        // Orden Detalle
        [HttpGet("CRMDetail/{id}")]
        public async Task<IActionResult> GetCRMDetail(int id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                SELECT
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    to_char(to_date(SUBSTRING(ord.""DocDueDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDueDate"",
                    to_char(to_date(SUBSTRING(ord.""DocDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""DocDate"",
                    to_char(to_date(SUBSTRING(ord.""CancelDate"", 0, 10), 'YYYY-MM-DD'), 'DD-MM-YYYY') as ""CancelDate"",

                    (case when ord.""CANCELED"" = 'Y' then 'Cancelado'
                    when ord.""DocStatus"" = 'O' then 'Abierto'
                    when ord.""DocStatus"" = 'C' then 'Cerrado'
                    else ord.""DocStatus"" end)  AS  ""DocStatus"",

                    ord.""DocTime"",
                    ord.""Address"",
                    ord.""Address2"",
                    ord.""DocCur"",
                    ord.""Comments"",
                    ord.""DocTotal"",
                    ord.""DocTotalFC"",
                    ord.""DocRate"",
                    payment.""PymntGroup"",
                    contact.""CardName"",
                    contact.""CardCode"",
                    contact.""CardFName"",
                    contact.""ListNum"",
                    employee.""SlpCode"",
                    employee.""SlpName"",
                    warehouse.""WhsCode"",
                    warehouse.""WhsName""
                FROM ORDR ord
                LEFT JOIN NNM1 series ON series.""Series"" = ord.""Series""
                LEFT JOIN OWHS warehouse ON warehouse.""WhsCode"" = series.""SeriesName""
                LEFT JOIN OSLP employee ON employee.""SlpCode"" = ord.""SlpCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OCRD contact ON contact.""CardCode"" = ord.""CardCode""
                WHERE ord.""DocEntry"" = '" + id + "' ");

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0];

            oRecSet.DoQuery(@"
                Select
                    ""LineNum"",
                    ""ItemCode"",
                    ""Dscription"",
                    ""Price"",
                    ""Currency"",
                    ""Quantity"",
                    ""UomCode"",
                    ""InvQty"",
                    ""OpenQty"",
                    ""UomEntry"",
                    ""UomCode2"",
                    ""LineTotal"",
                    ""U_CjsPsVr"",
                    ""TotalFrgn"",
                    ""Rate""
                From RDR1 Where ""DocEntry"" = '" + id + "'");
            oRecSet.MoveFirst();
            temp["RDR1"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            return Ok(temp);
        }

        // GET: api/Order/CRMOrderDaily
        [HttpGet("CRMOrderDaily")]
        public async Task<IActionResult> GetCRMOrderDaily() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"Select Count(*) as COUNT From ORDR");
            int CountAll = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0]["COUNT"].ToObject<int>();
            oRecSet.DoQuery(@"Select Count(*) as COUNT From ORDR Where ""DocDate"" = NOW()");
            int CountToday = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0]["COUNT"].ToObject<int>();
            oRecSet = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(new { CountAll, CountToday });
        }

        class OrderDeliveryOutputLineUom {
            public uint BaseEntry { get; set; }
            public string BaseUom { get; set; }
            public uint UomEntry { get; set; }
            public string UomCode { get; set; }
            public double BaseQty { get; set; }
        }
        class OrderDeliveryOutputLine {
            public string LineStatus { get; set; }
            public uint LineNum { get; set; }
            public string ItemCode { get; set; }
            public uint UomEntry { get; set; }
            public string WhsCode { get; set; }
            public string UomCode { get; set; }
            public double OpenInvQty { get; set; }
            public double OpenQty { get; set; }

            public string ItemName { get; set; }
            public char QryGroup42 { get; set; }
            public char QryGroup44 { get; set; }
            public char QryGroup45 { get; set; }
            public char ManBtchNum { get; set; }
            public double U_IL_PesMax { get; set; }
            public double U_IL_PesMin { get; set; }
            public double U_IL_PesProm { get; set; }
            public string U_IL_TipPes { get; set; }
            public double NumInSale { get; set; }
            public double NumInBuy { get; set; }
            public List<string> CodeBars { get; set; }
            public List<OrderDeliveryOutputLineUom> Uoms { get; set; }
        }
        class OrderDeliveryOutput {
            public uint DocEntry { get; set; }
            public uint DocNum { get; set; }
            [Required]
            public string DocStatus { get; set; }
            public string CardName { get; set; }
            public string CardCode { get; set; }
            public List<OrderDeliveryOutputLine> Lines { get; set; }
        }

        /// <summary>
        /// Get Order Detail to WMS App Delivery. This route return header and lines
        /// document, plus BarCodes and Uoms Detail.
        /// </summary>
        /// <param name="DocNum">DocNum. An Unsigned Integer that serve as Order Document identifier.</param>
        /// <returns>A Order Detail To Delivery</returns>
        /// <response code="200">Returns Order</response>
        /// <response code="204">No Order Found</response>
        /// <response code="400">Order Document Found, Document Close</response>
        // GET: api/Order/DeliverySAP/:DocNum
        [ProducesResponseType(typeof(OrderDeliveryOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [HttpGet("DeliverySAP/{DocNum}")]
        //[Authorize]
        public async Task<IActionResult> GetOrderToDeliverySAP(uint DocNum)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocStatus"",
                    ""CardName"",
                    ""CardCode""
                From ORDR WHERE ""DocNum"" = {DocNum};");

            if (oRecSet.RecordCount == 0)
            {
                return NoContent();
            }

            JToken order = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"][0];

            if (order["DocStatus"].ToString() != "O")
            {
                return BadRequest("Documento Cerrado");
            }

            oRecSet.DoQuery($@"
                Select
                    ""LineStatus"",
                    ""LineNum"",
                    Line.""ItemCode"",
                    ""UomEntry"",
                    ""WhsCode"",
                    ""UomCode"",
                    ""OpenInvQty"",
                    ""OpenQty"",
                    ""ItemName"",
                    ""QryGroup42"",
                    ""QryGroup44"",
                    ""QryGroup45"",
                    ""ManBtchNum"",
                    ""U_IL_PesMax"",
                    ""U_IL_PesMin"",
                    ""U_IL_PesProm"",
                    ""U_IL_TipPes"",
                    ""NumInSale""
                From RDR1 as Line
                JOIN OITM as Detail on Detail.""ItemCode"" = Line.""ItemCode""
                WHERE Line.""DocEntry"" = {order["DocEntry"]};");

            order["Lines"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            foreach (var line in order["Lines"])
            {

                oRecSet.DoQuery($@"
                    Select ""BcdCode""
                    From OBCD Where ""ItemCode"" = '{line["ItemCode"]}';");

                var temp = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"].Select(Q => (string)Q["BcdCode"]);
                line["CodeBars"] = JArray.FromObject(temp);

                oRecSet.DoQuery($@"
                    Select 
                        header.""BaseUom"" as ""BaseEntry"",
                        baseUOM.""UomCode"" as ""BaseUom"",
                        detail.""UomEntry"",
                        UOM.""UomCode"",
                        detail.""BaseQty""
                    From OUGP header
                    JOIN UGP1 detail ON header.""UgpEntry"" = detail.""UgpEntry""
                    JOIN OUOM baseUOM ON header.""BaseUom"" = baseUOM.""UomEntry""
                    JOIN OUOM UOM ON detail.""UomEntry"" = UOM.""UomEntry""
                    Where header.""UgpCode"" = '{line["ItemCode"]}';");
                line["Uoms"] = context.XMLTOJSON(oRecSet.GetAsXML())["OUGP"];
            }

            var output = order.ToObject<OrderDeliveryOutput>();
            return Ok(output);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



        // GET: api/Order/Delivery/5
        // Orden Con informacion extra para la entrega
        [HttpGet("Delivery/{id}")]
        public async Task<IActionResult> GetReception(int id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                Select
                    ""DocEntry"",
                    ""DocNum"",
                    ""DocStatus"",
                    ""CardName"",
                    ""CardCode""
                From ORDR WHERE ""DocNum"" = " + id);

            int rc = oRecSet.RecordCount;
            if (rc == 0)
            {
                return NotFound();
            }

            JToken order = context.XMLTOJSON(oRecSet.GetAsXML());
            order["AdmInfo"]?.Parent.Remove();
            order["ORDR"] = order["ORDR"][0];

            if (order["ORDR"]["DocStatus"].ToString() != "O")
            {
                return BadRequest("Documento Cerrado");
            }

            oRecSet.DoQuery(@"
                Select
                    ""LineStatus"",
                    ""LineNum"",
                    ""ItemCode"",
                    ""Dscription"",
                    ""UomEntry"",
                    ""WhsCode"",
                    ""UomCode"",
                    ""OpenInvQty"",
                    ""OpenQty""
                From RDR1 WHERE ""DocEntry"" = " + order["ORDR"]["DocEntry"]);

            order["RDR1"] = context.XMLTOJSON(oRecSet.GetAsXML())["RDR1"];

            foreach (var pro in order["RDR1"])
            {
                oRecSet.DoQuery(@"
                    Select
                        ""ItemCode"",
                        ""ItemName"",
                        ""QryGroup7"",
                        ""QryGroup41"",
                        ""QryGroup42"",
                        ""QryGroup43"",
                        ""QryGroup44"",
                        ""QryGroup45"",
                        ""ManBtchNum"",
                        ""U_IL_PesMax"",
                        ""U_IL_PesMin"",
                        ""U_IL_PesProm"",
                        ""U_IL_TipPes"",
                        ""NumInSale"",
                        ""NumInBuy""
                    From OITM Where ""ItemCode"" = '" + pro["ItemCode"] + "'");
                oRecSet.MoveFirst();
                pro["Detail"] = context.XMLTOJSON(oRecSet.GetAsXML())["OITM"][0];
                oRecSet.DoQuery(@"
                    Select
                        ""BcdEntry"",
                        ""BcdCode"",
                        ""BcdName"",
                        ""ItemCode"",
                        ""UomEntry""
                    From OBCD Where ""ItemCode"" = '" + pro["ItemCode"] + "'");
                oRecSet.MoveFirst();
                pro["CodeBars"] = context.XMLTOJSON(oRecSet.GetAsXML())["OBCD"];
            }
            return Ok(order);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // GET: api/Order/CRMList
        // Todas las Ordernes - Encabezado para lista CRM
        [HttpGet("CRMList")]
        public async Task<IActionResult> GetCRMList() {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""CardName"",
                    contact.""CardFName"",
                    person.""SlpName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }

        // GET: api/Order/CRMList
        // Todas las Ordernes - Encabezado para lista CRM
        [HttpGet("CRMAPPList/Sucursal/{id}")]
        public async Task<IActionResult> GetCRMAPPSucursalList(string id)
        {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""DocStatus"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""
                Where warehouse.""WhsCode"" = '" + id + @"' AND ord.""DocStatus"" = 'O' AND ord.""DocDate"" >= add_days(CURRENT_DATE, -3)");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }

        // GET: api/Order/CRMList
        // Todas las Ordernes - Encabezado para lista CRM
        [HttpGet("CRMList/Sucursal/{id}")]
        public async Task<IActionResult> GetCRMSucursalList(string id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""CardName"",
                    contact.""CardFName"",
                    person.""SlpName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""
                Where warehouse.""WhsCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }

        // GET: api/Order/CRMList/Contact/C00000001
        // Todas las Ordernes - Encabezado para lista CRM filtrado por cliente
        [HttpGet("CRMList/Contact/{id}")]
        public async Task<IActionResult> GetCRMListCLient(string id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery(@"
                Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""CANCELED"",
                    ord.""DocStatus"",
                    ord.""Series"",
                    ord.""SlpCode"",
                    ord.""CardName"",
                    person.""SlpName"",
                    warehouse.""WhsCode"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                Where ord.""CardCode"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }


        // GET: api/order/list
        // Ordenes Filtradas por dia
        [HttpGet("list/{date}")]
        public async Task<IActionResult> GetList(string date) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents items = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            List<Object> list = new List<Object>();

            oRecSet.DoQuery("Select * From ORDR Where \"DocDate\" = '" + date + "'");
            int rc = oRecSet.RecordCount;
            if (rc == 0) {
                return NotFound();
            }
            items.Browser.Recordset = oRecSet;
            items.Browser.MoveFirst();

            while (items.Browser.EoF == false) {
                JToken temp = context.XMLTOJSON(items.GetAsXML());
                temp["ORDR"] = temp["ORDR"][0];
                temp["RDR4"]?.Parent.Remove();
                temp["RDR12"]?.Parent.Remove();
                list.Add(temp);
                items.Browser.MoveNext();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(list);
        }

        // GET: api/order/list
        // Ordenes ultmos 3 dias Filtradas por empleado 
        [HttpGet("CRMAPP/list/{employee}")]
        public async Task<IActionResult> GetCRMAPPList(string id, int employee) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                 Select
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""DocStatus"",
                    contact.""CardFName"",
                    warehouse.""WhsName""
                From ORDR ord
                LEFT JOIN NNM1 serie ON ord.""Series"" = serie.""Series""
                LEFT JOIN OWHS warehouse ON serie.""SeriesName"" = warehouse.""WhsCode""
                LEFT JOIN OSLP person ON ord.""SlpCode"" = person.""SlpCode""
                LEFT JOIN OCRD contact ON ord.""CardCode"" = contact.""CardCode""
                Where ord.""DocStatus"" = 'O' AND ord.""DocDate"" >= add_days(CURRENT_DATE, -3) AND ord.""SlpCode"" =" + employee);

            oRecSet.MoveFirst();
            JToken orders = context.XMLTOJSON(oRecSet.GetAsXML())["ORDR"];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return Ok(orders);
        }

        // GET: api/Order/5
        // Orden Detalle
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery(@"
                SELECT
                    ord.""DocStatus"",
                    ord.""DocEntry"",
                    ord.""DocNum"",
                    ord.""DocDate"",
                    ord.""DocTime"",
                    ord.""DocDueDate"",
                    ord.""CancelDate"",
                    ord.""Address"",
                    ord.""Address2"",
                    ord.""DocCur"",
                    ord.""Comments"",
                    ord.""DocTotal"",
                    ord.""DocTotalFC"",
                    ord.""DocRate"",
                    payment.""PymntGroup"",
                    contact.""CardName"",
                    contact.""CardCode"",
                    contact.""CardFName"",
                    employee.""SlpCode"",
                    employee.""SlpName"",
                    warehouse.""WhsName""
                FROM ORDR ord
                LEFT JOIN NNM1 series ON series.""Series"" = ord.""Series""
                LEFT JOIN OWHS warehouse ON warehouse.""WhsCode"" = series.""SeriesName""
                LEFT JOIN OSLP employee ON employee.""SlpCode"" = ord.""SlpCode""
                LEFT JOIN OCTG payment ON payment.""GroupNum"" = ord.""GroupNum""
                LEFT JOIN OCRD contact ON contact.""CardCode"" = ord.""CardCode""
                WHERE ord.""DocEntry"" = '" + id + "' ");

            JToken temp = context.XMLTOJSON(oRecSet.GetAsXML());
            temp["ORDR"] = temp["ORDR"][0];
            temp["AdmInfo"]?.Parent.Remove();

            oRecSet.DoQuery(@"
                Select
                    ""ItemCode"",
                    ""Dscription"",
                    ""Price"",
                    ""Currency"",
                    ""Quantity"",
                    ""UomCode"",
                    ""InvQty"",
                    ""UomCode2"",
                    ""LineTotal"",
                    ""U_CjsPsVr"",
                    ""TotalFrgn"",
                    ""Rate""
                From RDR1 Where ""DocEntry"" = '" + id + "'");
            oRecSet.MoveFirst();
            JToken products = context.XMLTOJSON(oRecSet.GetAsXML());
            temp["RDR1"] = products["RDR1"];
            return Ok(temp);
        }

        private JToken limiteCredito(string CardCode, int Series, SAPContext context) {

            JToken result;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

            oRecSet.DoQuery($@"CALL ""ValidaCreditoMXM"" ('{CardCode}', '{Series}', 0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null) {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoMXM'}");
            }

            oRecSet.DoQuery($@"CALL ""ValidaCreditoENS"" ('{CardCode}', '{Series}', 0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null) {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoENS'}");
            }

            oRecSet.DoQuery($@"CALL ""ValidaCreditoTJ"" ('{CardCode}', '{Series}', 0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null) {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoTJ'}");
            }

            oRecSet.DoQuery($@"CALL ""ValidaCreditoSLR"" ('{CardCode}', '{Series}', 0)");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Recordset"][0];
            if (result["False"] == null) {
                return JObject.Parse(@"{ RESULT: 'True', AUTH: 'ValidaCreditoSLR'}");
            }
            return JObject.Parse(@"{ RESULT: 'False', AUTH: ''}");

        }

        private JToken facturasPendientes(string CardCode, int Series, SAPContext context) {

            JToken result;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            oRecSet.DoQuery($@"
                SELECT 'True' as Result, 'FacturasVencidasMXM' as Auth
                FROM Dummy
                WHERE '{CardCode}' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE AND T0.""DocStatus"" = 'O')
                AND  '{Series}' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 1))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty) {
                return result;
            }

            oRecSet.DoQuery($@"
                SELECT 'True' as Result, 'FacturasVencidasENS' as Auth
                FROM Dummy
                WHERE '{CardCode}' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE AND T0.""DocStatus"" = 'O')
                AND  '{Series}' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 4))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty) {
                return result;
            }

            oRecSet.DoQuery($@"
                SELECT 'True' as Result, 'FacturasVencidasTJ' as Auth
                FROM Dummy
                WHERE '{CardCode}' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE AND T0.""DocStatus"" = 'O')
                AND  '{Series}' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 2))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty) {
                return result;
            }

            oRecSet.DoQuery($@"
                SELECT 'True' as Result, 'FacturasVencidasSLR' as Auth
                FROM Dummy
                WHERE '{CardCode}' IN (SELECT Distinct T0.""CardCode"" FROM OINV T0 WHERE T0.""DocDueDate"" < CURRENT_DATE AND T0.""DocStatus"" = 'O')
                AND  '{Series}' IN (
                    SELECT T1.""Series"" FROM NNM1 T1
                    WHERE T1.""ObjectCode"" = 17
                    AND T1.""SeriesName"" IN (SELECT ""WhsCode"" FROM OWHS WHERE ""Location"" = 3))");
            oRecSet.MoveFirst();
            result = context.XMLTOJSON(oRecSet.GetAsXML())["Dummy"][0];
            if (result["RESULT"].ToString() != String.Empty) {
                return result;
            }
            return JObject.Parse(@"{ RESULT: 'False', AUTH: ''}");
        }

        private List<JToken> auth(string CardCode, int Series, SAPContext context) {
            List <JToken> result = new List<JToken>();
            JToken resultfact = facturasPendientes(CardCode, Series, context);
            JToken resultcredit = limiteCredito(CardCode, Series, context);
            result.Add(resultfact);
            result.Add(resultcredit);
            return result;
        }

        // POST: api/Order/SeparateOrder
        [HttpPost("SeparateOrder")]
        public IActionResult OrderSeparation([FromBody] CreateOrder value)
        {
            // List<JToken> ordersCreated = new List<JToken>();
            Queue<CreateOrder> orders = new Queue<CreateOrder>();
            // Only in CEDIS
            var productsMeatMXN = new List<OrderRow>();
            var productsMeatUSD = new List<OrderRow>();
            var productsNonMeatUSD = new List<OrderRow>();
            var productsNonMeatMXN = new List<OrderRow>();


            // Rest of warehouse
            var productsMXN = new List<OrderRow>();
            var productsUSD = new List<OrderRow>();


            if (value.series == 752)
            {
                foreach (var product in value.rows)
                {
                    if (product.currency == "USD" && product.meet == "Y")
                    {
                        productsMeatUSD.Add(product);

                    }
                    else if (product.currency == "USD" && product.meet != "Y")
                    {
                        productsNonMeatUSD.Add(product);

                    }
                    else if (product.currency == "MXN" && product.meet == "Y")
                    {

                        productsMeatMXN.Add(product);

                    }
                    else if (product.currency == "MXN" && product.meet != "Y")
                    {
                        productsNonMeatMXN.Add(product);
                    }
                }

                if (productsMeatUSD.Count != 0)
                {
                    var OrderProdMeatUSD = new CreateOrder
                    {
                        cardcode = value.cardcode,
                        currency = "USD",
                        type = "Orden de carne en dolares",
                        payment = value.payment,
                        comments = value.comments,
                        date = value.date,
                        series = value.series,
                        priceList = value.priceList,
                        rows = productsMeatUSD
                    };
                    orders.Enqueue(OrderProdMeatUSD);
                    //ordersCreated.Add(CreateOrderNew(OrderProdMeatUSD));
                }

                if (productsMeatMXN.Count != 0)
                {
                    var OrderProdMeatMXN = new CreateOrder
                    {
                        cardcode = value.cardcode,
                        currency = "MXN",
                        type = "Orden de carne en pesos",
                        payment = value.payment,
                        comments = value.comments,
                        date = value.date,
                        series = value.series,
                        priceList = value.priceList,
                        rows = productsMeatMXN
                    };
                    orders.Enqueue(OrderProdMeatMXN);
                    //ordersCreated.Add(CreateOrderNew(OrderProdMeatMXN));

                }

                if (productsNonMeatUSD.Count != 0)
                {
                    var OrderProdNonMeatUSD = new CreateOrder
                    {
                        cardcode = value.cardcode,
                        currency = "USD",
                        payment = value.payment,
                        type = "Orden de producto en dolares",
                        comments = value.comments,
                        date = value.date,
                        series = value.series,
                        priceList = value.priceList,
                        rows = productsNonMeatUSD
                    };
                    orders.Enqueue(OrderProdNonMeatUSD);
                    //ordersCreated.Add(CreateOrderNew(OrderProdNonMeatUSD));
                }
                if (productsNonMeatMXN.Count != 0)
                {
                    var OrderProdNonMeatMXN = new CreateOrder
                    {
                        cardcode = value.cardcode,
                        currency = "MXN",
                        type = "Orden de producto en pesos",
                        payment = value.payment,
                        comments = value.comments,
                        date = value.date,
                        series = value.series,
                        priceList = value.priceList,
                        rows = productsNonMeatMXN
                    };
                    orders.Enqueue(OrderProdNonMeatMXN);
                    //ordersCreated.Add(CreateOrderNew(OrderProdNonMeatMXN));
                }

            }
            else
            {
                foreach (var product in value.rows)
                {
                    if (product.currency == "USD")
                    {
                        productsUSD.Add(product);
                    }
                    else
                    {
                        productsMXN.Add(product);
                    }
                }
                if (productsUSD.Count != 0)
                {
                    var OrderProdUSD = new CreateOrder
                    {
                        cardcode = value.cardcode,
                        currency = "USD",
                        type = "Orden en dolares",
                        payment = value.payment,
                        comments = value.comments,
                        date = value.date,
                        series = value.series,
                        priceList = value.priceList,
                        rows = productsUSD
                    };
                    orders.Enqueue(OrderProdUSD);
                    //ordersCreated.Add(CreateOrderNew(OrderProdUSD));
                }
                if (productsMXN.Count != 0)
                {
                    var OrderProdMXN = new CreateOrder
                    {
                        cardcode = value.cardcode,
                        currency = "MXN",
                        type = "Orden en pesos",
                        payment = value.payment,
                        comments = value.comments,
                        date = value.date,
                        series = value.series,
                        priceList = value.priceList,
                        rows = productsMXN
                    };
                    orders.Enqueue(OrderProdMXN);
                    //ordersCreated.Add(CreateOrderNew(OrderProdMXN));
                }
            }
            // Descomentar para probar el regreso de ordenes
            //orders.Dequeue();
            //return Ok(orders);

            //Recorre la fila mientras aun queden ordenes en ella o hasta que se hayan completado 3 vueltas.
            //Cada iteracion recorre la fila llamando al metodo CreateOrderNew para intentar agregar la nueva orden a base de datos SAP.
            //Verifica que se agrego en caso contrario saca la orden de la fila y la vuelve a ingresar al final de esta.
            //En el caso de que sea agregada con exito solo saca la orden de la fila
            int Iteracion = 0;
            while (orders.Count > 0 && Iteracion < 12)
            {
                var orderJToken = CreateOrderNew(orders.Peek());
                if (orderJToken["RESULT"].ToString() == "False" && orderJToken["Status"].ToString() == "409")
                {
                    return Conflict(new { AUTH = orderJToken["Reason"] });

                }
                else if (orderJToken["RESULT"].ToString() == "False" && orderJToken["Status"].ToString() == "400")
                {
                    CreateOrder order = orders.Peek();
                    order.type = "Orden en " + order.currency + " No se pudo crear: " + orderJToken["Reason"].ToString();
                    orders.Enqueue(order);
                    orders.Dequeue();
                }
                else
                {
                    orders.Dequeue();
                }
                Iteracion++;
            }
            if (orders.Count > 0)
            {
                return Ok(orders);
            }
            return Ok();
            //foreach(var order in ordersCreated)
            //{
            //    if (order["RESULT"].ToString() == "False" && order["Status"].ToString() == "409")
            //    {
            //        return Conflict(new { AUTH = order["Reason"] });
            //
            //    }
            //    else if (order["RESULT"].ToString() == "False" && order["Status"].ToString() == "400")
            //    {
            //        return BadRequest(new { error = order["Reason"] });
            //
            //    }
            //}
        }

        public JToken CreateOrderNew(CreateOrder value)
        {
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            if (value.auth == 0 && value.payment != 19)
            {
                List<JToken> resultAuth = new List<JToken>();

                if (value.payment == 8)
                {
                    resultAuth.Add(facturasPendientes(value.cardcode, value.series, context));

                    if (resultAuth[0]["RESULT"].ToString() == "True")
                    {
                        return JObject.Parse(@"{ RESULT: 'False', Status: '409', Reason: '" + resultAuth[0]["AUTH"].ToString() + "'}");
                    }
                } else
                {
                    resultAuth = auth(value.cardcode, value.series, context);

                    if (resultAuth[0]["RESULT"].ToString() == "True" || resultAuth[1]["RESULT"].ToString() == "True")
                    {
                        return JObject.Parse(@"{ RESULT: 'False', Status: '409', Reason: '" + resultAuth[0]["AUTH"].ToString() + resultAuth[1]["AUTH"].ToString() + "'}");

                    }
                }
            }

            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            SAPbobsCOM.BusinessPartners contact = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);

            oRecSet.DoQuery($@"
                Select
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 17 AND serie.""Series"" = {value.series};");

            if (oRecSet.RecordCount == 0)
            {
                return JObject.Parse(@"{ RESULT: 'False', Status: '400', Reason: 'Error en sucursal'}");
            }

            string warehouse = (string)oRecSet.Fields.Item("WhsCode").Value;

            order.CardCode = value.cardcode;
            order.Series = value.series;
            order.DocCurrency = value.currency;
            order.DocDueDate = value.date;
            order.PaymentGroupCode = value.payment;

            if (!contact.GetByKey(value.cardcode))
            {
                string error = context.oCompany.GetLastErrorDescription();
                return JObject.Parse(@"{ RESULT: 'False', Status: '400', Reason: '" + error + "' }");
            }

            String temp = (String)contact.UserFields.Fields.Item("U_B1SYS_MainUsage").Value;
            if (temp != String.Empty)
            {
                order.UserFields.Fields.Item("U_SO1_02USOCFDI").Value = temp;
            }
            temp = (String)contact.UserFields.Fields.Item("U_IL_MetPago").Value;
            if (temp != String.Empty)
            {
                order.UserFields.Fields.Item("U_SO1_02METODOPAGO").Value = temp;
            }
            temp = (String)contact.UserFields.Fields.Item("U_IL_ForPago").Value;
            if (temp != String.Empty)
            {
                order.UserFields.Fields.Item("U_SO1_02FORMAPAGO").Value = temp;
            }

            for (int i = 0; i < value.rows.Count; i++)
            {

                order.Lines.ItemCode = value.rows[i].code;
                order.Lines.WarehouseCode = warehouse;

                oRecSet.DoQuery($@"
                    Select
                        ""Currency"",
                        ""Price""
                    FROM ITM1
                    WHERE ""ItemCode"" = '{value.rows[i].code}' 
                    AND ""PriceList"" = {value.priceList};");

                if (oRecSet.RecordCount == 0)
                {
                    return JObject.Parse(@"{ RESULT: 'False', Status: '400', Reason: 'Error en Lista de Precio'}");
                }

                double Price = (double)oRecSet.Fields.Item("Price").Value;
                string Currency = (string)oRecSet.Fields.Item("Currency").Value;

                if (value.rows[i].uom == -2)
                {
                    order.Lines.UnitPrice = Price;
                }
                else
                {
                    order.Lines.UnitPrice = Price * value.rows[i].equivalentePV;
                }
                order.Lines.Currency = Currency;

                if (value.rows[i].uom == -2)
                {
                    order.Lines.UoMEntry = 185;
                    order.Lines.UserFields.Fields.Item("U_CjsPsVr").Value = value.rows[i].quantity;
                    order.Lines.Quantity = value.rows[i].quantity * value.rows[i].equivalentePV;
                }
                else
                {
                    order.Lines.Quantity = value.rows[i].quantity;
                    order.Lines.UoMEntry = value.rows[i].uom;
                }

                order.Lines.Add();
            }

            order.Comments = value.comments;

            if (order.Add() == 0)
            {
                return JObject.Parse(@"{ RESULT: 'True', Status: '200', Reason: ''}");
            }
            else
            {
                string error = context.oCompany.GetLastErrorDescription();
                return JObject.Parse(@"{ RESULT: 'False', Status: '400', Reason: '" + error + "' }");
            }
        }



        // POST: api/Order
        // Creacion de Orden
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateOrder value) {

            //SAPMulti SAPMultiInstance = HttpContext.RequestServices.GetService(typeof(SAPMulti)) as SAPMulti;

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            //SAPContext context = SAPMultiInstance.GetCurrentInstance();

            //SAPContext context;

            //do
            //{
            //    context = SAPMultiInstance.IncrementInstance();

            //} while (context.oCompany.InTransaction);


            //if (context == null)
            //{
            //    return UnprocessableEntity("Servicio saturado. Favor de reintentar en un minuto.");
            //}

           
            //context.oCompany.StartTransaction();

            if (value.auth == 0 && value.payment != 19)
            {
                List<JToken> resultAuth = new List<JToken>();

                if (value.payment == 8)
                {
                    resultAuth.Add(facturasPendientes(value.cardcode, value.series, context));
                    if (resultAuth[0]["RESULT"].ToString() == "True")
                    {
                        //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                        return Conflict(resultAuth);
                    }
                }
                else
                {
                    resultAuth = auth(value.cardcode, value.series, context);
                    if (resultAuth[0]["RESULT"].ToString() == "True" || resultAuth[1]["RESULT"].ToString() == "True")
                    {
                        //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                        return Conflict(resultAuth);
                    }
                }
            }

            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            SAPbobsCOM.BusinessPartners contact = (SAPbobsCOM.BusinessPartners)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners);

            oRecSet.DoQuery($@"
                Select
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 17 AND serie.""Series"" = {value.series};");

            if (oRecSet.RecordCount == 0) {
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return BadRequest(new { error = "Error en sucursal" });
            }

            string warehouse = (string)oRecSet.Fields.Item("WhsCode").Value;

            order.CardCode = value.cardcode;
            order.Series = value.series;
            order.DocCurrency = value.currency;
            order.DocDueDate = value.date;
            order.PaymentGroupCode = value.payment;

            if (!contact.GetByKey(value.cardcode)) {
                string error = context.oCompany.GetLastErrorDescription();
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return BadRequest(new { error });
            }

            String temp = (String)contact.UserFields.Fields.Item("U_B1SYS_MainUsage").Value;
            if (temp != String.Empty) {
                order.UserFields.Fields.Item("U_SO1_02USOCFDI").Value = temp;
            }
            temp = (String)contact.UserFields.Fields.Item("U_IL_MetPago").Value;
            if (temp != String.Empty) {
                order.UserFields.Fields.Item("U_SO1_02METODOPAGO").Value = temp;
            }
            temp = (String)contact.UserFields.Fields.Item("U_IL_ForPago").Value;
            if (temp != String.Empty) {
                order.UserFields.Fields.Item("U_SO1_02FORMAPAGO").Value = temp;
            }
           
            for (int i = 0; i < value.rows.Count; i++) {

                order.Lines.ItemCode = value.rows[i].code;
                order.Lines.WarehouseCode = warehouse;
                
                oRecSet.DoQuery($@"
                    Select
                        ""Currency"",
                        ""Price""
                    FROM ITM1
                    WHERE ""ItemCode"" = '{value.rows[i].code}' 
                    AND ""PriceList"" = {value.priceList};");

                if(oRecSet.RecordCount == 0) {
                    //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                    return BadRequest(new { error = "Error en Lista de Precio" });
                }

                double Price = (double)oRecSet.Fields.Item("Price").Value;
                string Currency = (string)oRecSet.Fields.Item("Currency").Value;

                if (value.rows[i].uom == -2) {
                    order.Lines.UnitPrice = Price ;
                } else {
                    order.Lines.UnitPrice = Price * value.rows[i].equivalentePV;
                }
                order.Lines.Currency = Currency;

                if (value.rows[i].uom == -2) {
                    order.Lines.UoMEntry = 185;
                    order.Lines.UserFields.Fields.Item("U_CjsPsVr").Value = value.rows[i].quantity;
                    order.Lines.Quantity = value.rows[i].quantity * value.rows[i].equivalentePV;
                } else {
                    order.Lines.Quantity = value.rows[i].quantity;
                    order.Lines.UoMEntry = value.rows[i].uom;
                }

                order.Lines.Add();
            }

            order.Comments = value.comments;

            if (order.Add() == 0) {
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                return Ok();
            } else {
                string error = context.oCompany.GetLastErrorDescription();
                //context.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                return BadRequest(new { error });
            }
        }

        // POST: api/Order
        // Creacion de Orden
        [HttpPost("Retail")]
        public async Task<IActionResult> PostRetail([FromBody] CreateOrderRetail value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;

            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            SAPbobsCOM.Items items = (SAPbobsCOM.Items)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems);

            oRecSet.DoQuery(@"
                Select
                    warehouse.""WhsCode"",
                    warehouse.""WhsName"",
                    serie.""Series""
                From OWHS warehouse
                LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                Where serie.""ObjectCode"" = 17 AND serie.""Series"" = " + value.series);
            oRecSet.MoveFirst();
            string warehouse = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"][0]["WhsCode"].ToString();

            order.CardCode = value.cardcode;
            order.Series = value.series;
            order.DocCurrency = value.currency;
            order.DocDueDate = value.date;
            order.Address = value.address;
            order.Address2 = "";

            for (int i = 0; i < value.rows.Count; i++) {
                order.Lines.ItemCode = value.rows[i].code;
                order.Lines.WarehouseCode = warehouse;

                items.GetByKey(value.rows[i].code);

                for (int j = 0; j < items.PriceList.Count; j++) {
                    items.PriceList.SetCurrentLine(j);
                    if (items.PriceList.PriceList == 18) {
                        if (value.rows[i].uom == -2) {
                            order.Lines.UnitPrice = items.PriceList.Price;
                        } else {
                            order.Lines.UnitPrice = items.PriceList.Price * value.rows[i].equivalentePV;
                        }
                        order.Lines.Currency = items.PriceList.Currency;
                        break;
                    }
                }

                if (value.rows[i].uom == -2) {
                    order.Lines.UoMEntry = 6;
                    order.Lines.UserFields.Fields.Item("U_CjsPsVr").Value = value.rows[i].quantity;
                    order.Lines.Quantity = value.rows[i].quantity * value.rows[i].equivalentePV;
                } else {
                    order.Lines.Quantity = value.rows[i].quantity;
                    order.Lines.UoMEntry = value.rows[i].uom;
                }

                order.Lines.Add();
            }

            order.Comments = value.comments;
            int result = order.Add();
            if (result == 0) {
                return Ok(new { value = context.oCompany.GetNewObjectKey() });
            } else {
                string error = context.oCompany.GetLastErrorDescription();
                return BadRequest(new { error });
            }
        }

        // PUT: api/Order/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateOrder value) {

            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Documents order = (SAPbobsCOM.Documents)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
            SAPbobsCOM.Items items = (SAPbobsCOM.Items)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems);
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            
            if (order.GetByKey(id)) {
                oRecSet.DoQuery(@"
                    Select
                        warehouse.""WhsName"",
                        warehouse.""WhsCode"",
                        serie.""Series""
                    From OWHS warehouse
                    LEFT JOIN NNM1 serie ON serie.""SeriesName"" = warehouse.""WhsCode""
                    Where serie.""Series"" = '" + order.Series + "'");
                oRecSet.MoveFirst();
                string warehouse  = context.XMLTOJSON(oRecSet.GetAsXML())["OWHS"][0]["WhsCode"].ToString();
                order.Lines.Add();
                for (int i = 0; i < value.newProducts.Count; i++) {
                    order.Lines.ItemCode = value.newProducts[i].code;
                    order.Lines.WarehouseCode = warehouse;

                    items.GetByKey(value.newProducts[i].code);

                    for (int j = 0; j < items.PriceList.Count; j++) {
                        items.PriceList.SetCurrentLine(j);
                        if (items.PriceList.PriceList == 2) {
                            if (value.newProducts[i].uom == -2) {
                                order.Lines.UnitPrice = items.PriceList.Price;
                            } else {
                                order.Lines.UnitPrice = items.PriceList.Price * value.newProducts[i].equivalentePV;
                            }
                            order.Lines.Currency = items.PriceList.Currency;
                            break;
                        }
                    }

                    if (value.newProducts[i].uom == -2) {
                        order.Lines.UoMEntry = 6;
                        order.Lines.UserFields.Fields.Item("U_CjsPsVr").Value = value.newProducts[i].quantity;
                        order.Lines.Quantity = value.newProducts[i].quantity * value.newProducts[i].equivalentePV;
                    } else {
                        order.Lines.Quantity = value.newProducts[i].quantity;
                        order.Lines.UoMEntry = value.newProducts[i].uom;
                    }

                    order.Lines.Add();
                }
                

                for (int i = 0; i < value.ProductsChanged.Count; i++) {
                    order.Lines.SetCurrentLine(value.ProductsChanged[i].LineNum);
                    if (order.Lines.Quantity != value.ProductsChanged[i].quantity) {
                        order.Lines.Quantity = value.ProductsChanged[i].quantity;
                    }

                    if (order.Lines.UoMEntry != value.ProductsChanged[i].uom) {
                        order.Lines.UoMEntry = value.ProductsChanged[i].uom;
                        items.GetByKey(order.Lines.ItemCode);
                        for (int j = 0; j < items.PriceList.Count; j++) {
                            items.PriceList.SetCurrentLine(j);
                            if (items.PriceList.PriceList == 2) {
                                order.Lines.UnitPrice = items.PriceList.Price * value.ProductsChanged[i].equivalentePV;
                                order.Lines.Currency = items.PriceList.Currency;
                                break;
                            }
                        }
                    }
                }
                
                int result = order.Update();
                if (result == 0) {
                    return Ok();
                } else {
                    string error = context.oCompany.GetLastErrorDescription();
                    return BadRequest(new { error });
                }

            }

            return BadRequest(new { error = "No Existe Documento" });
        }

    }
}
