using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace TrainingApp.Models;

public class EquipmentItem : ReactiveObject
{
    public string Name { get; set; }
    [Reactive]
    public bool IsSelected { get; set; }
    
}
