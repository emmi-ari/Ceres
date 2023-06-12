namespace FrontsEditor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            if (args.Length > 0)
            {
                if (File.Exists(args[0]))
                    Application.Run(new FrontsEditorMain(args[0]));
                else
                    MessageBox.Show($"Can not find file {args[0]}. Please make sure it exists and you have sufficient rights to access the file."
                        , "Fronts Editor"
                        , MessageBoxButtons.OK
                        , MessageBoxIcon.Error);
            }
            else
                Application.Run(new FrontsEditorMain());
        }
    }
}