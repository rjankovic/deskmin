using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using _min.Models;
using _min.Common;
using CC = _min.Common.Constants;
using CE = _min.Common.Environment;

namespace _min.Models
{
    public class ArchitectQuestionEventArgs : EventArgs
    {
        public string questionText { get; private set; }
        public Dictionary<string, object> options { get; private set; }

        public ArchitectQuestionEventArgs(string qText, Dictionary<string, object> options) {
            questionText = qText;
            this.options = options;
        }
    }

    public delegate void ArchitectQuestion(IArchitect sender, ArchitectQuestionEventArgs e);

    public class ArchitectureErrorEventArgs : EventArgs
    {
        public string message { get; private set; }
        public string tableName { get; private set; }
        public IPanel panel { get; private set; }
        public IField field { get; private set; }
        public IControl control { get; set; }

        public ArchitectureErrorEventArgs(string message, string tableName) {
            this.message = message;
            this.tableName = tableName;
            this.panel = null;
            this.field = null;
            this.control = null;
        }

        public ArchitectureErrorEventArgs(string message, IPanel panel, IField field)
            :this(message, panel.tableName)
        {
            this.panel = panel;
            this.field = field;
        }

        public ArchitectureErrorEventArgs(string message, IPanel panel, IControl control)
            : this(message, panel.tableName)
        {
            this.panel = panel;
            this.control = control;
        }
    }

    public delegate void ArchitectureError(IArchitect sender, ArchitectureErrorEventArgs e);

    
    public class ArchitectWarningEventArgs : ArchitectureErrorEventArgs
    {
        public ArchitectWarningEventArgs(string message, string tableName)
            : base(message, tableName)
        { }
    }

    public delegate void ArchitectWarning(IArchitect sender, ArchitectWarningEventArgs e);

    
    public class ArchitectNoticeEventArgs : ArchitectureErrorEventArgs
    {
        public ArchitectNoticeEventArgs(string message, string tableName = null)
            : base(message, tableName)
        { }
    }

    public delegate void ArchitectNotice(IArchitect sender, ArchitectNoticeEventArgs e);


    public delegate IPanel AsyncProposeCaller();

    class Architect : IArchitect
    {
        public class ColumnDisplayComparer : IComparer<DataColumn> 
        {

            public int Compare(DataColumn x, DataColumn y)
            {
                if (x == y || x.DataType == y.DataType) return 0;
                if (x == null) return 1;
                if (y == null) return -1;
                if (x.DataType == typeof(string) && y.DataType == typeof(string))
                    return y.MaxLength - x.MaxLength;
                if(x.DataType == typeof(string)) return -1;
                if(y.DataType == typeof(string)) return 1;
                if(x.DataType == typeof(DateTime)) return -1;
                if(x.DataType == typeof(DateTime)) return 1;
                if(x.DataType == typeof(int)) return -1;
                if(y.DataType == typeof(int)) return 1;
                return 0;
            }
        }

        public object questionAnswer;
        public event ArchitectQuestion Question;
        public event ArchitectureError Error;
        public event ArchitectWarning Warning;
        public event ArchitectNotice Notice;

        private ISystemDriver systemDriver;
        private IStats stats;
        private List<IM2NMapping> mappings;
        private List<IM2NMapping> usedMappings;
        private Dictionary<PanelTypes, int> panelTypeIdMp;
        private Dictionary<FieldTypes, int> fieldTypeIdMap;


        public Architect(ISystemDriver system, IStats stats) {
            this.stats = stats;
            this.systemDriver = system;
            this.mappings = stats.findMappings();
            this.usedMappings = new List<IM2NMapping>();
            panelTypeIdMp = system.PanelTypeNameIdMap();
            fieldTypeIdMap = system.FieldTypeNameIdMap();
            questionAnswer = null;
        }

        private List<string> DisplayColOrder(string tableName)      
            // the order in which columns will be displayed in summary tables & M2NMapping, covers all columns
        {
            DataColumnCollection cols = stats.columnTypes(tableName);

            List<string> res = new List<string>();
            List<DataColumn> colList = new List<DataColumn>(from DataColumn col in cols select col);
            ColumnDisplayComparer comparer = new ColumnDisplayComparer();
            colList.Sort(comparer);
            return new List<string>(from col in colList select col.ColumnName);
        }

        public IPanel getArchitectureInPanel()
        {
            return systemDriver.getArchitectureInPanel();
        }
        /// <summary>
        /// propose the editable panel for a table, if table will probably not be self-editable
        /// (such as an M2N mapping), return null
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns>IPanel (or null)</returns>
        public IPanel proposeForTable(string tableName)
        {
            // dont care for indexes for now

            DataColumnCollection cols = stats.columnTypes(tableName);
            List<IFK> FKs = stats.foreignKeys(tableName);
            List<string> PKCols = stats.primaryKeyCols(tableName);
            while(PKCols.Count == 0) 
            {
                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add("Try again", 1);
                options.Add("Ommit table", 2);

                ArchitectQuestionEventArgs args = new ArchitectQuestionEventArgs(
                    "The table " + tableName + " does not have a primary key defined and therefore "
                    + "cannot be used in the administration. Is this intentional or will you add the primary key? (OMMIT TABLE)",
                    options);
                Question(this, args);
                //int answer = (int)questionAnswer;
                int answer = 2;
                if (answer == 1)
                    PKCols = stats.primaryKeyCols(tableName);
                else
                    return null;
            }


            if (cols.Count == 2 && FKs.Count == 2) return null; // seems like mapping table
            // FK ~> mapping ?
            
            List<IField> fields = new List<IField>();

            foreach (IM2NMapping mapping in mappings) {
                if (mapping.myTable == tableName && !usedMappings.Exists(m => mapping == m))
                    // && (stats.TableCreation(mapping.myTable) > stats.TableCreation(mapping.refTable)
                    // the later-created table would get to edit the mapping
                    // but I`d better ask the user
                    
                {
                    Dictionary<string, object> options = new Dictionary<string,object>();
                    options.Add("Include in this panel", 1);
                    options.Add("Include in this panel only", 2);
                    options.Add("Do not include", 3);
                    ArchitectQuestionEventArgs args = new ArchitectQuestionEventArgs(
                        "While proposing administration panel for the table " + mapping.myTable
                        + ", the system found out that the table " + mapping.mapTable
                        + " is likely to  be a M to N mapping between this table and " + mapping.refTable
                        + ". Do you want to include an interface to manage this mapping in this panel? (INCLUDE IN THIS PANEL ONLY)",
                        options);
                    Question(this, args); // ask questions!
                    //int answer = (int)questionAnswer;
                    int answer = 2;
                    if(answer == 1 || answer == 2){
                    // no potentional field from cols is removed by this, though
                    List<string> displayColOrder = DisplayColOrder(mapping.refTable);
                    mapping.displayColumn = displayColOrder[0];
                    fields.Add(new M2NMappingField(0, mapping.myColumn, fieldTypeIdMap[FieldTypes.M2NMapping],
                        FieldTypes.M2NMapping.ToString(), 0, mapping));
                    }
                    if(answer == 2){
                        usedMappings.Add(mapping);
                    }
                }
            }

            // standard FKs
            foreach(IFK actFK in FKs){
                List<string> displayColOrder = DisplayColOrder(actFK.refTable);
                actFK.displayColumn = displayColOrder[0];
                fields.Add(new FKField(0, actFK.myColumn, fieldTypeIdMap[FieldTypes.FK], 
                    FieldTypes.FK.ToString(), 0, actFK));
                cols.Remove(actFK.myColumn);    // will be edited as a foreign key
            }
            // editable fields in the order as defined in table; don`t edit AI
            foreach (DataColumn col in cols) {
                PropertyCollection validation = new PropertyCollection();
                
                if (!col.ExtendedProperties.ContainsKey(CC.COLUMN_EDITABLE)) continue;
                if(!col.AllowDBNull) validation.Add(CC.RULES_REQUIRED, true);
                FieldTypes fieldType;  // default => standard textBox
              
                if(col.DataType == typeof(string)){
                        if(col.MaxLength <= 255) fieldType = FieldTypes.Varchar;
                        else fieldType = FieldTypes.Text;
                }
                else if(col.DataType == typeof(int) || col.DataType == typeof(long) || col.DataType == typeof(short)){
                        fieldType = FieldTypes.Ordinal;
                        validation.Add(fieldType.ToString(), true);
                }
                else if(col.DataType == typeof(float) || col.DataType == typeof(double)){
                        fieldType = FieldTypes.Decimal;
                        validation.Add(fieldType.ToString(), true);
                }
                else if(col.DataType == typeof(bool)){
                        fieldType = FieldTypes.Bool;
                }
                else if(col.DataType == typeof(DateTime)){
                    if(col.ExtendedProperties.ContainsKey(CC.FIELD_DATE_ONLY))
                        fieldType = FieldTypes.Date;
                        // should DATETIME, BUT DATETIME is usually used for date only...or is it?
                    else fieldType = FieldTypes.Date;
                    validation.Add(fieldType.ToString(), true);
                }
                else if (col.DataType == typeof(Enum)) {
                    // cannot happen, since column properties are taken from Stats
                    if (!col.ExtendedProperties.ContainsKey(CC.COLUMN_ENUM_VALUES))
                        throw new Exception("Missing enum options for field " + col.ColumnName);
                    fieldType = FieldTypes.Enum;    
                }
                else
                {
                    throw new Exception("Unrecognised column type " + col.DataType.ToString());
                }
                fields.Add(new Field(0, col.ColumnName, fieldTypeIdMap[fieldType], 
                    fieldType.ToString(), 0, col.ExtendedProperties, validation));  // don`t add any properties, just copy from Stat
            }
            fields.OrderBy(x => ((int)(x.attr[CC.FIELD_POSITION])));
            // setup controls as properies
            PropertyCollection controlProps = new PropertyCollection();
            PropertyCollection viewProps = new PropertyCollection();
            viewProps.Add(CC.PANEL_NAME, tableName + " Editation");

            List<Control> controls = new List<Control>();

            string actionName = UserAction.View.ToString();
            controlProps.Add(actionName, actionName);
            controlProps.Add(actionName + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 1);

            actionName = UserAction.Insert.ToString();
            controlProps.Add(actionName, actionName);
            controlProps.Add(actionName + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 3);
            
            actionName = UserAction.Update.ToString();
            controlProps.Add(actionName, actionName);
            controlProps.Add(actionName + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 5);

            actionName = UserAction.Delete.ToString();
            controlProps.Add(actionName, actionName);
            controlProps.Add(actionName + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 5);
            
            foreach(string actName in Enum.GetNames(typeof(UserAction))){
                if(controlProps.ContainsKey(actName)){
                    controls.Add(new Control(actName, (UserAction)Enum.Parse(typeof(UserAction), actName)));
                }
            }
            List<IControl> controlsAsIControl = new List<IControl>(controls);
            
            //set additional properties
            // the order of fields in edit form (if defined), if doesn`t cover all editable columns, display the remaining
            // at the begining, non-editable columns are skipped
            //viewProps[CC.PANEL_DISPLAY_COLUMN_ORDER] = String.Join(",", DisplayColOrder(tableName));

            Panel res = new Panel(tableName, 0, panelTypeIdMp[PanelTypes.Editable], PanelTypes.Editable.ToString(), 
                new List<IPanel>(), fields, controlsAsIControl, PKCols, null, viewProps, controlProps);
            return res;
        }

        /// <summary>
        /// Ignores mappings, displays first 4 columns in a classic NavTable 
        /// or the first display column in a NavTree if a self-referential FK is present.
        /// navtable includes edit and delete action buttons, second control - insert button
        /// </summary>
        /// <param name="tableName">string</param>
        /// <returns>IPanel</returns>
        private IPanel proposeSummaryPanel(string tableName) {
            DataColumnCollection cols = stats.columnTypes(tableName);
            List<IFK> FKs = stats.foreignKeys(tableName);
            // a table with more than one self-referential FK is improbable
            List<IFK> selfRefs = new List<IFK>(from FK in FKs where FK.myTable == FK.refTable select FK as IFK);
            List<string> PKCols = stats.primaryKeyCols(tableName);
            IFK selfRefFK = null;
            // strict hierarchy structure validation - not nice => no Tree

            string hierarchyExplanation = "(For a tree-like navigation within view, the table must have exactly one FK pointing "
                + " to it`s PK column. (It can of course have other FKs refering to other tables.))"; 
            
            if (selfRefs.Count > 1)
                Warning(this, new ArchitectWarningEventArgs(
                    "Unexpected hierarchy table structure for " + tableName + " - multiple self-referential columns. " 
                    + hierarchyExplanation, tableName));
            else
            if (selfRefs.Count == 1 && PKCols.Count == 1){
                selfRefFK = selfRefs.First();

                if (PKCols.Count > 1)
                {
                    Warning(this, new ArchitectWarningEventArgs(
                    "Hierarchical table " + tableName + " does have a signle-column PK. "
                    + hierarchyExplanation, tableName));
                    selfRefFK = null;       // remove the selfRefFK => as if there was no hierarchy
                }
                if (selfRefFK.refColumn != PKCols[0])
                {
                    selfRefFK = null;
                    Warning(this, new ArchitectWarningEventArgs("The self-referential FK in table " 
                        + tableName + " must refer to the PK column. " 
                        + hierarchyExplanation, tableName));
                }
            }
            List<string> displayColOrder = DisplayColOrder(tableName);

            PropertyCollection controlProps = new PropertyCollection();
            PropertyCollection displayProps = new PropertyCollection();

            displayProps.Add(CC.PANEL_DISPLAY_COLUMN_ORDER, String.Join(",", displayColOrder));
            displayProps.Add(CC.PANEL_NAME, tableName);
            controlProps.Add(UserAction.View.ToString() + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 1);
            IControl control;
            DataTable controlTab = new DataTable();
            PanelTypes panelType;

            List<IField> fields = new List<IField>();
            
            if(selfRefFK == null){
                //displayProps.Add(CC.NAVTAB_COLUMNS_DISLAYED, CC.NAVTAB_COLUMNS_DISLAYED_DEFAULT);
                // table takes first four display-suitable fields; the above is not neccessary, in Navtable Columns are fields
                foreach(string column in displayColOrder.Take(CC.NAVTAB_COLUMNS_DISLAYED_DEFAULT)){
                    //controlTab.Columns.Add(column);
                    if (FKs.Any(x => x.myColumn == column))
                    {
                        IFK myFK = FKs.Find(x => x.myColumn == column);
                        fields.Add(new FKField(0, column, fieldTypeIdMap[FieldTypes.Varchar], FieldTypes.Varchar.ToString(),
                            0, myFK));
                    }
                    else
                    {
                        fields.Add(new Field(0, column, fieldTypeIdMap[FieldTypes.Varchar], FieldTypes.Varchar.ToString(), 0));
                    }
                    fields.OrderBy(x => displayColOrder.IndexOf(x.column)); 
                    // to maintain the order from displayColOrder without further properties
                }
                control = new Control(controlTab, PKCols, UserAction.Update);
                panelType = PanelTypes.NavTable;
            }
            else {
                controlProps.Add(CC.CONTROL_HIERARCHY_SELF_FK_COL, selfRefFK.myColumn);
                controlTab.Columns.Add(PKCols[0]);
                controlTab.Columns.Add(selfRefFK.myColumn);
                controlTab.Columns.Add(displayColOrder[0]);
                control = new TreeControl(controlTab, PKCols[0], selfRefFK.myColumn, displayColOrder[0], UserAction.Update);
                panelType = PanelTypes.NavTree;
            }

            List<IControl> controls = new List<IControl>();
            controls.Add(control);

            return new Panel(tableName, 0, panelTypeIdMp[panelType], panelType.ToString(), 
                new List<IPanel>(), fields, controls, PKCols, null, displayProps, controlProps); 
        }



        /// <summary>
        /// get both edit and summary panel proposal for editable tables 
        /// and create base Panel with MenuDrop field for each editable table
        /// with 2 children pointing to insert action and summary table view;
        /// also saves it to systemDB, because it needs the panelIDs to 
        /// build navigation upon them.
        /// The proposal should always pass proposal check.
        /// </summary>
        /// <returns>IPanel</returns>
        public IPanel propose()
        {
            Notice(this, new ArchitectNoticeEventArgs("Starting proposal..."));
            if (systemDriver.ProposalExists()) {
                Dictionary<string, object> options = new Dictionary<string,object>();
                options.Add("Repropose", true);
                options.Add("Edit", false);
                Question(this, new ArchitectQuestionEventArgs("A proposal already exists for this project. \n" +
                    "Do you want to remove it and propose again or edit the existing proposal? (REMOVE)", options));
                //bool repropose = (bool)questionAnswer;
                bool repropose = true;
                if (!repropose)
                {
                    return systemDriver.getArchitectureInPanel();
                }
                systemDriver.ClearProposal();
            }

            List<string> tables = stats.TableList();
            List<IPanel> baseChildren = new List<IPanel>();
            foreach (string tableName in tables) {
                Notice(this, new ArchitectNoticeEventArgs("Exploring table \"" + tableName + "\"..."));
                IPanel editPanel = proposeForTable(tableName);
                if (editPanel != null)
                {      // editable panel available - add summary panel
                    Notice(this, new ArchitectNoticeEventArgs("Table \"" + tableName + "\" will be editable."));
                    IPanel summaryPanel = proposeSummaryPanel(tableName);
                    Notice(this, new ArchitectNoticeEventArgs("Proposed summary navigation panel for table \"" + tableName + "\"."));
                    baseChildren.Add(editPanel);
                    baseChildren.Add(summaryPanel);
                }
                else 
                {
                    Notice(this, new ArchitectNoticeEventArgs("Table \"" + tableName + "\" is probably NOT suitable for direct management."));
                }
            }
            Notice(this, new ArchitectNoticeEventArgs("Creating navigation base with " +
                baseChildren.Count + " options (2 per table)."));
            IPanel basePanel = new Panel(null, 0, panelTypeIdMp[PanelTypes.MenuDrop], PanelTypes.MenuDrop.ToString(), 
                baseChildren, null, null, null);
            Notice(this, new ArchitectNoticeEventArgs("Updating database..."));
            systemDriver.addPanel(basePanel);
            DataTable basePanelTreeControlData = systemDriver.fetchBaseNavControlTable();
            IControl basePanelTreeControl = new TreeControl(basePanelTreeControlData, "id_panel", "id_parent", "name", UserAction.View);
            
            // now children have everything set, even parentid 
            // as the parent was inserted first, his id was set and they took it from the object

            List<IControl> addedList = new List<IControl>();
            addedList.Add(basePanelTreeControl);
            basePanel.AddControls(addedList);

            basePanel.AddControlAttr(CC.NAV_THROUGH_PANELS, true);
            basePanel.AddViewAttr(CC.PANEL_NAME, "Main menu");
            basePanel.AddControlAttr(UserAction.View.ToString() + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 1);
            
            systemDriver.updatePanel(basePanel, false); // children aren`t changed, just adding a control for the basePanel
            return basePanel;
        }

        /// <summary>
        /// checks if edited fields exist in webDB and their types are adequate, checks constraints on FKs and mapping tables,
        /// also checks if controls` dataTables` columns exist and everything that has to be inserted in DB is a required field,
        /// whether attributes don`t colide and every panel has something to display.
        /// As it goes through the Panel, it fires an ArchitectureError event.
        /// </summary>
        /// <param name="proposalPanel"></param>
        /// <param name="recursive">run itself on panel children</param>
        /// <returns>true if no errors found, true othervise</returns>
        public bool checkPanelProposal(IPanel proposalPanel, bool recursive = true)
            // non-recursive checking after the initial check - after panel modification
        {
            string messageBeginning = "In panel " + ( proposalPanel.viewAttr.ContainsKey(CC.PANEL_NAME) ? 
                ( proposalPanel.viewAttr[CC.PANEL_NAME] + " (" + proposalPanel.tableName + ") " ) :
                ( "of type " + proposalPanel.typeName + " for " + proposalPanel.tableName ) ) + ": ";
            
            DataColumnCollection cols = stats.columnTypes(proposalPanel.tableName);
            if (cols.Count == 0)
            {
                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "table not found or has 0 columns",
                    proposalPanel.tableName));
                return false;
            }

            List<IFK> FKs = stats.foreignKeys(proposalPanel.tableName);

            bool good = true;
            if (proposalPanel.typeName == PanelTypes.Editable.ToString() 
                || proposalPanel.typeName == PanelTypes.NavTable.ToString())       
                // this is indeed the only panelType containing fields
            {
                bool isNavTable = proposalPanel.typeName == PanelTypes.NavTable.ToString();
                foreach (IField field in proposalPanel.fields)
                {
                    if (field.typeName == FieldTypes.Holder.ToString())
                        continue;
                    if (!cols.Contains(field.column))
                    {
                        Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the column " + field.column +
                            "managed by the field does not exist in table", proposalPanel, field));
                        good = false;
                    }
                    else
                    {
                        if (!(field is IFKField) && !(field is IM2NMapping) && !isNavTable)     // NavTable won`t be edited in the panel
                        {
                            PropertyCollection r = field.rules;
                            if (cols[field.column].AllowDBNull == false && !r.ContainsKey(CC.RULES_REQUIRED))
                            {
                                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the column " + field.column
                                    + " cannot be set to null, but the coresponding field is not required", proposalPanel, field));
                                good = false;
                            }

                            if ((r.ContainsKey(CC.RULUES_DECIMAL) || r.ContainsKey(CC.RULES_ORDINAL))
                                && !(typeof(long).IsAssignableFrom(cols[field.column].DataType)))
                            {
                                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the column " + field.column
                                + " is of type " + cols[field.column].DataType.ToString()
                                + ", thus cannot be edited as a decimalnumber", proposalPanel, field));
                                good = false;
                            }

                            if ((r.ContainsKey(CC.RULES_DATE) || r.ContainsKey(CC.RULES_DATETIME))
                                && !(cols[field.column].DataType == typeof(DateTime)))
                            {
                                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the column " + field.column
                                + " is not a date / datetime, thus cannot be edited as a date", proposalPanel, field));
                                good = false;
                            }
                        }
                        else if (field is IM2NMappingField)
                        {
                            // just cannot occur in a NavTable, but just in case...
                            if (isNavTable) throw new Exception("Cannot display a M2NMapping in NavTable");
                            IM2NMapping thisMapping = ((IM2NMappingField)field).mapping;
                            if (!mappings.Contains(thisMapping))
                            {        
                                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the schema " +
                                    "does not define an usual M2NMapping batween tables " + thisMapping.myTable +
                                    " and " + thisMapping.refTable + " using " + thisMapping.mapTable + 
                                    " as a map table", proposalPanel, field));
                                good = false;
                            }
                        }
                        else if( field is IFKField) 
                        {
                            IFK fieldFK = ((IFKField)field).fk;
                            if (!FKs.Contains(fieldFK)) 
                            {        
                                Error(this, new ArchitectureErrorEventArgs(messageBeginning + "the column " + field.column
                                + " is not a foreign key representable by the FK field", proposalPanel, field));
                                good = false;
                            }
                        }
                    }
                }
            }

            // controls...

            PanelTypes PTEnum = (PanelTypes)Enum.Parse(typeof(PanelTypes), proposalPanel.typeName);
            if (PTEnum == PanelTypes.NavTable || PTEnum == PanelTypes.NavTree || PTEnum == PanelTypes.MenuDrop) { 
                if(proposalPanel.controlAttr.ContainsKey(CC.NAV_THROUGH_PANELS))
                {
                    if(proposalPanel.tableName != null)
                    {
                        Error(this, new ArchitectureErrorEventArgs(messageBeginning + "Panel that navigates through panels " 
                            + "cannot have tableName set", proposalPanel.tableName));
                            good = false;
                    }
                }
                else if (proposalPanel.controlAttr.ContainsKey(CC.NAV_THROUGH_RECORDS))
                {
                    if (!proposalPanel.controlAttr.ContainsKey(CC.NAV_PANEL_ID)) 
                    {
                        Error(this, new ArchitectureErrorEventArgs(messageBeginning + "Navigation panel "
                            + "that does not navigate through panels must have target panel id set", 
                            proposalPanel, proposalPanel.controls[0]));
                        good = false;
                    }
                }
            }

            // TODO & TODO & TODO (CONTROLS & OTHER PROPERTIES)
            // OR allow the admin-user take valid steps only (?)

            if (recursive) foreach (IPanel child in proposalPanel.children) {
                good = good && checkPanelProposal(child, true);
            }
            return good;
        }

        public bool checkPanelProposal(int panelId, bool recursive = true)  // on first load / reload request; also in production
        {
            IPanel proposalPanel = systemDriver.getPanel(panelId, true);
            return checkPanelProposal(proposalPanel, true);
        }

        public bool checkProposal() 
        {
            IPanel proposalPanel = systemDriver.getArchitectureInPanel();
            return checkPanelProposal(proposalPanel, true);
        }
    }
}