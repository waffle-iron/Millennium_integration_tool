using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mconverter
{
    class protocol
    {
        public Int64 NUMBERKART { get; set; }
        public Int64 NUMBERISSL { get; set; }
        public String OPISANIE { get; set; }/* TTEXTOPISANIE = BLOB SUB_TYPE 1 SEGMENT SIZE 80 */
        public string SAKL { get; set; }
        public byte[] EXPERTSAKL { get; set; } /* TTEXTOPISANIE = BLOB SUB_TYPE 1 SEGMENT SIZE 80 */
        public string STUDY_ID { get; set; }
        public DateTime IMAGES_UPDATE_DATETIME { get; set; }
        public DateTime DATEISSL { get; set; }
        public DateTime TIMEISSL { get; set; }

    }
}
