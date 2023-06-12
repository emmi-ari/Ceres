using Newtonsoft.Json.Linq;

using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace FrontsEditor
{
    public partial class FrontsEditorMain : Form
    {
        public Dictionary<string, string> Members { get; set; }
        public Dictionary<string, string> CustomFronts { get; set; }

        public FrontsEditorMain(string path)
        {
            InitializeComponent();
            string[] relations = InitChecks(path);

            Members = GetRelationProperty(relations[0]);
            CustomFronts = GetRelationProperty(relations[1]);

            foreach (var member in Members)
            {
                listBox1.Items.Add(member);
            }
        }

        public FrontsEditorMain()
        {
            InitializeComponent();

            OpenFileDialog fileDialog = new()
            {
                Filter = "JSON files|*.json|All|*.*",
                Multiselect = false
            };
            DialogResult x = fileDialog.ShowDialog(this);

            if (x == DialogResult.Cancel)
                this.Close();

            string[] relations = InitChecks(fileDialog.FileName);
            Members = GetRelationProperty(relations[0]);
            CustomFronts = GetRelationProperty(relations[1]);
        }

        #region ctor dependencies
        private string[] InitChecks(string path)
        {
            dynamic deserializedJson = JObject.Parse(WaitFor(File.ReadAllTextAsync(path)));
            string members = deserializedJson["apparyllis.members"]?.ToString();
            string customFronts = deserializedJson["apparyllis.customFronts"]?.ToString();

            if (string.IsNullOrEmpty(members))
                MessageBox.Show(this, "Could not find Member entries in the provided config file"
                    , "Fronts Editor - Configuration file Error"
                    , MessageBoxButtons.OK
                    , MessageBoxIcon.Error);

            if (string.IsNullOrEmpty(customFronts))
                if (MessageBox.Show(this, "Could not find custom front entries. Proceed?"
                    , "Fronts Editor - No custom fronts"
                    , MessageBoxButtons.YesNo
                    , MessageBoxIcon.Question) == DialogResult.No)
                    this.Close();

            string[] retVal = { members, customFronts };
            return retVal;
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Useless without class properties")]
        private Dictionary<string, string> GetRelationProperty(string memberIdsDeserialized)
        {
            string[] memberIdArray = memberIdsDeserialized.Split(' ');
            Dictionary<string, string> memberIdNames = new(memberIdArray.Length);
            foreach (string memberId in memberIdArray)
            {
                string[] memberIdAux = memberId.Split('_');
                memberIdNames.Add(memberIdAux[1].Trim(), memberIdAux[0].Trim());
            }
            return memberIdNames;
        }
        #endregion

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        internal static A WaitFor<A>(Task<A> task)
            => Task.Run(async () => { return await task; }).Result;
    }
}