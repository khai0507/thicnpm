using System.ComponentModel.DataAnnotations;

namespace DocTask.Core.Dtos.Tasks;

public class CreateTaskDto
{
    [Required(ErrorMessage = "Title required", AllowEmptyStrings = false)]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Title required", AllowEmptyStrings = false)]
    public string Description { get; set; }

    [Required(ErrorMessage = "StartDate required")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "DueDate required")]
    public DateTime? DueDate { get; set; }
}