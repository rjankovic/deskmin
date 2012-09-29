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
    class Architect : IArchitect
    {
        public class ColumnDisplayComparer : IComparer<DataColumn> 
        {

            public int Compare(DataColumn x, DataColumn y)
            {
                if (x.DataType == typeof(string) && y.DataType == typeof(string))
                    return y.MaxLength - x.MaxLength;
                if(x.DataType == string) return -1;
                if(y.DataType == typeof(string)) return 1;
                if(x.DataType == typeof(DateTime)) return -1;
                if(x.DataType == typeof(DateTime)) return 1;
                if(x.DataType == typeof(int)) return -1;
                if(y.DataType == typeof(int)) return 1;
                return 0;
            }
        }

        private ISystemDriver system;
        private IStats stats;
        private List<IM2NMapping> mappings;
        private Dictionary<PanelTypes, int> panelTypeIdMp;
        private Dictionary<FieldTypes, int> fieldTypeIdMap;

        Architect(ISystemDriver system, IStats stats) {
            this.stats = stats;
            this.system = system;
            this.mappings = stats.findMappings();
            panelTypeIdMp = system.PanelTypeNameIdMap();
            fieldTypeIdMap = system.FieldTypeNameIdMap();
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
            return system.getArchitectureInPanel();
        }

        public IPanel proposeForTable(string tableName)
        {
            // dont care for indexes for now
            DataColumnCollection cols = stats.columnTypes(tableName);
            List<IFK> FKs = stats.foreignKeys(tableName);
            List<string> PKCols = stats.primaryKeyCols(tableName);
            if (cols.Count == 2 && FKs.Count == 2) return null; // seems like mapping table
            // FK ~> mapping ?
            
            List<IField> fields = new List<IField>();

            foreach (IM2NMapping mapping in mappings) {
                if (mapping.myTable == tableName && 
                    (stats.TableCreation(mapping.myTable) > stats.TableCreation(mapping.refTable)))
                    // the later-created table will get to edit the mapping
                {
                    // no potentional field from cols is removed by this, though
                    List<string> displayColOrder = DisplayColOrder(mapping.refTable);
                    mapping.displayColumn = displayColOrder[0];
                    fields.Add(new M2NMappingField(0, mapping.myColumn, fieldTypeIdMap[FieldTypes.M2NMapping],
                        FieldTypes.M2NMapping.ToString(), 0, mapping));
                    break;
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
                PropertyCollection attr = new PropertyCollection();
                
                if (col.AutoIncrement || !((bool)col.ExtendedProperties[CC.COLUMN_EDITABLE])) continue;
                if(!col.AllowDBNull) validation.Add(CC.RULES_REQUIRED, true);
                FieldTypes fieldType;  // default => standard textBox
              
                if(col.DataType == typeof(string)){
                        if(col.MaxLength <= 255) fieldType = FieldTypes.Varchar
                        else fieldType = FieldTypes.Text;
                }
                else if(col.DataType == typeof(int) || col.DataType == typeof(long) || col.DataType == typeof(short)){
                        fieldType = FieldTypes.Ordinal;
                        validation.Add(fieldType, true);
                }
                else if(col.DataType == typeof(float) || col.DataType == typeof(double)){
                        fieldType = FieldTypes.Decimal;
                        validation.Add(fieldType, true);
                }
                else if(col.DataType == typeof(bool)){
                        fieldType = FieldTypes.Bool;
                }
                else if(col.DataType == typeof(DateTime){
                    if(col.ExtendedProperties.ContainsKey(CC.FIELD_DATE_ONLY))
                        fieldType = FieldTypes.Date;
                        // should DATETIME, BUT DATETIME is usually used for date only...or is it?
                    else fieldType = FieldTypes.Date;
                    validation.Add(fieldType, true);
                }
                else{
                    throw new Exception("Unrecognised column type " + col.DataType.ToString());
                }
                fields.Add(new Field(0, col.ColumnName, fieldTypeIdMap[fieldType], 
                    fieldType.ToString(), 0, col.ExtendedProperties, validation));  // don`t add any properties, just copy from Stat
            }
            fields.OrderBy(x => ((int)(x.attr[CC.FIELD_POSITION])));
            // setup controls as properies
            PropertyCollection controlProps = new PropertyCollection();
            PropertyCollection viewProps = new PropertyCollection();

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
            
            foreach(string actName in Enum.GetNames(typeof(UserAction)){
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
            if (selfRefs.Count > 1)
                throw new Exception("Unexpected hierarchy table structure for " + tableName + " - multiple self-referential columns");
            else if (selfRefs.Count == 1){
                selfRefFK = selfRefs.First();
                if(PKCols.Count > 1) throw new Exception("Hierarchical table " + tableName + " must have a signle-column PK");
                if(selfRefFK.refColumn != PKCols[0]) throw new Exception("The self-referential FK in table " 
                    + tableName + " must refer to the PK column." );
            }
            List<string> displayColOrder = DisplayColOrder(tableName);

            PropertyCollection controlProps = new PropertyCollection();
            PropertyCollection displayProps = new PropertyCollection();

            displayProps.Add(CC.PANEL_DISPLAY_COLUMN_ORDER, String.Join(",", displayColOrder));
            controlProps.Add(UserAction.View.ToString() + CC.CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX, 1);
            IControl control;
            DataTable controlTab = new DataTable();
            PanelTypes panelType;
            if(selfRefFK == null){
                displayProps.Add(CC.NAVTAB_COLUMNS_DISLAYED, CC.NAVTAB_COLUMNS_DISLAYED_DEFAULT);        // table takes first four display-suitable fields
                foreach(string column in displayColOrder.Take(CC.NAVTAB_COLUMNS_DISLAYED_DEFAULT)){
                    controlTab.Columns.Add(column);
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
                new List<IPanel>(), new List<IField>(), controls, PKCols, null, displayProps, controlProps); 
        }

        /// <summary>
        /// get both edit and summary panel proposal for editable tables, 
        /// create base Panel with MenuDrop field for each editable table
        /// with 2 children pointing to insert action and summary table view
        /// </summary>
        /// <returns></returns>
        public IPanel propose()
        {
            List<string> tables = stats.TableList();
            foreach (string tableName in tables) {
                IPanel editPanel = proposeForTable(tableName);
                if(editPanel != null){      // editable panel available - add summary panel
                    IPanel summaryPanel = proposeSummaryPanel(tableName);
                }
            }
                    
        }

        public bool checkPanelProposal(IPanel proposal, bool recursive = true)
        {
            throw new NotImplementedException();
        }

        public bool checkPanelProposal(int panelId, bool recursive = true)  // on first load / reload request; also in production
        {
            throw new NotImplementedException();
        }
    }
}