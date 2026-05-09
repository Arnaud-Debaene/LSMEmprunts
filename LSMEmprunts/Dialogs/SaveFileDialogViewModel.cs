namespace LSMEmprunts.Dialogs
{
    public sealed class SaveFileDialogViewModel
    {
        public bool OverwritePrompt { get; set; } = true;
        public bool CreatePrompt { get; set; } = false;
        public string FileName { get; set; }
        public string InitialDirectory { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public bool AddExtension { get; set; } = true;
        public string Title { get; set; }
        public bool ValidateNames { get; set; }
    }
       
}
