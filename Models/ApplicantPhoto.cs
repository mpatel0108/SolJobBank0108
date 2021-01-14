using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace sol_Job_Bank.Models
{
    public class ApplicantPhoto : UploadedPhoto
    {
        [Display(Name = "Applicant")]
        public int ApplicantID { get; set; }

        public Applicant Applicant { get; set; }
    }
}
