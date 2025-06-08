using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace TrainingApp.Models
{
    public class MuscleGroupItem(string name) : ReactiveObject
    {
        [Reactive] public string Name { get; set; } = name;
        [Reactive] public bool IsSelected { get; set; } = false;
    }
}