using System.Threading.Tasks;

namespace LSMEmprunts.Dialogs
{
    /// <summary>
    /// A ViewModel for a system standard Open File Dialog
    /// </summary>
    public sealed class OpenFileDialogViewModel
    {
        public bool Multiselect { get; set; } = false;
        public bool ReadOnlyChecked { get; set; }
        public bool ShowReadOnly { get; set; } = true;
        public string FileName { get; set; }
        public string[] FileNames { get; set; }
        public string Filter { get; set; } = string.Empty;
        public string InitialDirectory { get; set; } = string.Empty;
        public string SafeFileName { get; set; }
        public string[] SafeFileNames { get; set; }
        public string Title { get; set; }
        public bool ValidateNames { get; set; }

        internal readonly TaskCompletionSource<bool> ResultPromise = new TaskCompletionSource<bool>();
        public Task<bool> Completion => ResultPromise.Task;
    }

    
}
