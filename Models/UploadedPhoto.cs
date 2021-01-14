using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace sol_Job_Bank.Models
{
    public class UploadedPhoto
    {
        public UploadedPhoto()
        {
            PhotoContentFull = new PhotoContent();
            PhotoContentThumb = new PhotoContent();
        }
        public int ID { get; set; }

        [StringLength(255, ErrorMessage = "The name of the file cannot be more than 255 characters.")]
        [Display(Name = "File Name")]
        public string FileName { get; set; }

        [InverseProperty("PhotoFull")]
        public PhotoContent PhotoContentFull { get; set; }

        [InverseProperty("PhotoThumb")]
        public PhotoContent PhotoContentThumb { get; set; }
    }
}
