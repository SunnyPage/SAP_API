using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SAP_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SAP_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        
        // GET api/<InvoiceController>/5
        [HttpGet("{DocEntry}")]
        public IActionResult GetInvoice(string DocEntry)
        {
            
            SAPContext context = HttpContext.RequestServices.GetService(typeof(SAPContext)) as SAPContext;
            SAPbobsCOM.Recordset oRecSet = (SAPbobsCOM.Recordset)context.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
            JToken invoice;
            oRecSet.DoQuery(@"
                Select
                    Invoice.""DocNum"",
                    Invoice.""DocDate"",
                    Invoice.""CardCode"",
                    Invoice.""DocCur"",
                    Invoice.""DocRate"",
                    Invoice.""DocNum""|| to_char(Invoice.""DocDate"", 'YYYYMMDD')||Invoice.""CardCode"" as ""CodBar"",
                    Invoice.""DocTotalSy"",
                    Serie.""SeriesName""
                    From OINV Invoice
                    JOIN NNM1 Serie ON Serie.""Series"" = Invoice.""Series""
                    Where Invoice.""DocNum"" = '" + DocEntry + "'");
            if (oRecSet.RecordCount == 0)
            {
                // Handle no Existing Invoice
                return NotFound(@"La factura con el numero "+DocEntry+" no se encuentra registrada,favor de verificar");
            }
            invoice = context.XMLTOJSON(oRecSet.GetAsXML())["OINV"][0];

            return Ok(invoice);
        }
    }
}
