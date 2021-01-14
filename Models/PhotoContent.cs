using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace sol_Job_Bank.Models
{
    public class PhotoContent
    {
        public int ID { get; set; }

        [ScaffoldColumn(false)]
        public byte[] Content { get; set; }

        [StringLength(255)]
        public string MimeType { get; set; }

        [ForeignKey("PhotoFull")]
        public int? PhotoFullId { get; set; }
        public UploadedPhoto PhotoFull { get; set; }

        [ForeignKey("PhotoThumb")]
        public int? PhotoThumbId { get; set; }
        public UploadedPhoto PhotoThumb { get; set; }
    }
}
