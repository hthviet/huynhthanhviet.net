using System.ComponentModel.DataAnnotations;

namespace vget.Models
{
    public class InputModel
    {
        [Required]
        [RegularExpression("^https://www.fshare.vn/file/.*")]
        public string URL { get; set; }
    }
}
