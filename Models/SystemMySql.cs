using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _min.Models;
using MySql.Data.MySqlClient;
using _min.Interfaces;
using System.Data;
using _min.Common;
using CC = _min.Common.Constants;
using CE = _min.Common.Environment;

namespace _min.Models
{
    class SystemDriverMySql : BaseDriverMySql, ISystemDriver
    {
        public SystemDriverMySql(string connstring, DataTable logTable = null)
            : base(connstring, logTable)
        { }


        public void saveLog()
        {
            if (logTable.IsInitialized) { 
                foreach(DataRow r in logTable.Rows){
                    query("INSERT INTO log_db VALUES ", r);
                }
                logTable.Rows.Clear();
            }
        }

        public void logUserAction(System.Data.DataRow data)
        {
            query("INSERT INTO log_users VALUES ", data);
        }

        public bool isUserAuthorized(int panelId, UserAction act)
        {
            int rightsPossessed = (int)fetchSingle("SELECT access FROM access_rights WHERE id_user = "
                + CE.user.id + " AND id_project = ", CE.project.id);
            string rightsCol;
            switch (act) { 
                case UserAction.View:
                    rightsCol = "view";
                    break;
                case UserAction.Insert:
                    rightsCol = "insert";
                    break;
                default:
                    rightsCol = "modify";
                    break;
            }
            return (bool)fetchSingle("SELECT ", rightsPossessed, " >= ", rightsCol, 
                "_rights_reqiured FROM panels WHERE id_panel = ", panelId);
        }

        public void doRequests()
        {
            DataTable requests = fetchAll("SELECT * FROM requests WHERE id_project IS NULL OR id_project = ",
                Common.Environment.project.id, " AND `when` > NOW()");
            
            // TODO fire requests
            
            var requestsToRemove = from req in requests.AsEnumerable() where req["repeat"] == null select req["id_request"];
            query("DELETE FROM requests WHERE id_request IN ", requestsToRemove);
            var requestsToRepeat = from req in requests.AsEnumerable() where req["repeat"] != null select req;
            foreach(DataRow row in requestsToRepeat)
                query("UPDATE requests SET `when` = ADDTIME(`when`,", row["repeat"], ")");
        }

        private List<Field> PanelFields(int idPanel){
            DataTable tbl = fetchAll("SELECT * FROM fields JOIN field_types USING(id_field) WHERE id_panel = ", idPanel);
            List<Field> res = new List<Field>();
            foreach(DataRow row in tbl.Rows){
                
                int typeId = (int) row["id_type"];
                int fieldId = (int)row["id_field"];

                PropertyCollection properties = new PropertyCollection();
                PropertyCollection rules = new PropertyCollection();
                PropertyCollection controlOptions = new PropertyCollection();
                DataTable propsTab = fetchAll("SELECT name, val, concerns FROM fields_meta WHERE id_field = ", fieldId);
                foreach(DataRow propRow in propsTab.Rows)
                    switch((string)propRow["concerns"]){
                        case "view":
                            properties.Add(propRow["name"], propRow["val"]);
                            break;
                        case "validation":
                            rules.Add(propRow["name"], propRow["val"]);
                            break;
                        case "controls":
                            controlOptions.Add(propRow["name"], propRow["val"]);
                            break;
                        default:
                            throw new Exception("Cannot handle metadata about " + propRow["concerns"].ToString() + " (yet).");
                    }
                
                properties.Add("caption", row["caption"] as string);
                
                string typeName = row["type_name"] as string;
                if(!controlOptions.ContainsKey("isFK") && !controlOptions.ContainsKey("isM2NMapping")){
                    res.Add(new Field(fieldId, (string)row["column_name"], typeId, (string)row["type_name"], idPanel, properties, rules));
                    continue;   // just a standard field
                }
                
                //  FK or M2NMapping
                string myTable = fetchSingle("SELECT table_name FORM panels WHERE id_panel = ", idPanel) as string;
                string myColumn = row["column_name"] as string;
                string refTable = controlOptions[CC.FIELD_REF_TABLE] as string;
                string refColumn = controlOptions[CC.FIELD_REF_COLUMN] as string;
                string displayColumn = controlOptions[CC.FIELD_DISPLAY_COLUMN] as string;
                
                if(controlOptions.ContainsKey("isFK")){     // foreign key
                    FKMySql fk = new FKMySql(myTable, myColumn, refTable, refColumn, displayColumn);
                    res.Add(new FKField(fieldId, myColumn, typeId, typeName, idPanel, fk, properties, rules));
                }
                
                //  M2NMapping
                string mapTable = controlOptions[CC.FIELD_MAP_TABLE] as string;
                string mapMyColumn = controlOptions[CC.FIELD_MAP_MY_COLUMN] as string;
                string mapRefColumn = controlOptions[CC.FIELD_REF_COLUMN] as string;

                M2NMappingMySql mapping = new M2NMappingMySql(myTable, myColumn, refTable, refColumn, mapTable, displayColumn, mapMyColumn, mapRefColumn);
                res.Add(new M2NMappingField(fieldId, myColumn, typeId, typeName, idPanel, mapping, properties, rules));
            }

            return res;
        }
        
        private DataSet getPanelHierarchy() {   //TODO cache?
            DataTable panels = fetchAll("SELECT id_panel, table_name, id_parent FROM panels " 
            + " JOIN panel_types USING(id_panel) WHERE id_project = ", CE.project.id);
            panels.TableName = "panels";
            DataColumn[] tablePK = new DataColumn[1] {panels.Columns["id_panel"]};
            panels.PrimaryKey = tablePK;
            DataSet ds = new DataSet();
            ds.Tables.Add(panels);
            ds.Relations.Add("panelHierarchy", ds.Tables["panels"].Columns["id_panel"], ds.Tables["panels"].Columns["id_parent"]);
            return ds;
        }

        private List<IPanel> getPanelChildren(IPanel basePanel, bool recursive = true)
        {

            DataSet hierarchy = getPanelHierarchy();
            DataRow myPanelRow = hierarchy.Tables["panels"].Rows.Find(basePanel.panelId);
            DataRow[] childPanels = ((DataRow)myPanelRow).GetChildRows("panelHierarchy");
            
            IPanel currentChild;
            List<IPanel> res = new List<IPanel>();
            foreach(DataRow row in childPanels){
                currentChild = getPanel((int)(row["id_panel"]), recursive, basePanel);
                if(recursive)
                    currentChild.AddChildren(getPanelChildren(currentChild, true));
            }
            return res;
        }

        public IPanel getPanel(int panelId, bool recursive = true, IPanel parent = null)
        {
            DataRow panelRow = fetch("SELECT * FROM panels "
            + " JOIN panel_types USING(id_panel) WHERE id_panel = ", panelId);

            List<Field> fields = PanelFields(panelId);
            List<string> PKColNames = new List<string>(((string)panelRow["pk_column_names"]).Split(','));

            PropertyCollection viewProperties = new PropertyCollection();
            PropertyCollection controlProperties = new PropertyCollection();
            // !! attr to handle global validation rules needed?
            
            //if((int?)panelRow["holder"] != null) viewProperties["holder"] = (int)panelRow["holder"];

            DataTable propsTab = fetchAll("SELECT name, val, concerns FROM panels_meta WHERE id_panel = ", panelId);  
            
            foreach(DataRow row in propsTab.Rows)
                switch(row["concerns"] as string){
                    case CC.ATTR_VIEW:
                        viewProperties.Add(row["name"], row["val"]);
                        break;
                    case CC.ATTR_CONTROLS:
                        controlProperties.Add(row["name"], row["val"]);
                        break;
                    default:
                        throw new Exception("Cannot handle panel properties concerning " 
                            + row["concerns"] as string + " (yet). ");
                }
            
            //determine the controls
            List<Control> controls = new List<Control>();       // TODO rewrite into enum over PanelTypes
            switch((string)panelRow["type_name"]){
                case Constants.PANEL_EDITABLE:      // standard edit window
                    foreach(UserAction action in Enum.GetValues(typeof(UserAction))){
                        if(action == UserAction.View) continue;
                        string actionName = action.ToString();
                        string controlCaption = (string)controlProperties[actionName];      //!!
                        if(controlCaption != CC.CONTROL_DISABLED && 
                                (int)controlProperties[actionName + "ALR"] <= CE.user.rights){  // Authorization Level Required
                            controls.Add(new Control(controlCaption, action));
                        }
                    }
                    break;
                case CC.PANEL_MONITOR:   // just display, no controls
                    break;               
                case CC.PANEL_MENUDROP:  // the contol is the whole panel, do not load data
                case CC.PANEL_MENUTABS:       // will be a field type
                case CC.PANEL_NAVTABLE:
                case CC.PANEL_NAVTREE:
                    string localActionName = UserAction.View.ToString();

                    //  duplicite information about controls - in controlProperties and as Control objects. For Proposal phase use only

                    if((string)controlProperties[localActionName] != CC.CONTROL_DISABLED &&     // there is sth to show and user can see it
                        (int)controlProperties[localActionName + "ALR"] <= (int)CE.user.rights){
                        DataTable controlTabStruct = new DataTable();
                        List<string> displayColumns = new List<string>(     // table structure for summary
                            (viewProperties[CC.PANEL_DISPLAY_COLUMN_ORDER] as string).Split(','));      // already in database - only first few columns
                        string panelTypeName = (string)panelRow["type_name"];
                        if (panelTypeName == CC.PANEL_NAVTABLE)     // usual navigation table
                        {
                            foreach(string s in displayColumns.Take((int)viewProperties[CC.NAVTAB_COLUMNS_DISLAYED])){
                                controlTabStruct.Columns.Add(s);
                            }
                            controls.Add(new Control(controlTabStruct, PKColNames, UserAction.View));
                        }
                        else if (panelTypeName == CC.PANEL_MENUTABS)
                        {  // will take data from children (captions as panel Names) 
                            controlTabStruct.Columns.Add("childPanelName");
                            controls.Add(new Control(controlTabStruct, "childPanelName", UserAction.View));
                        }
                        else
                        {       // gotta be navTree or menuDrop => treeControl
                            if (PKColNames.Count > 1)    //  must have single PKcol
                                throw new Exception("Tree hierarchies must have single-column primay key");
                            controlTabStruct.Columns.Add(PKColNames[0]);
                            controlTabStruct.Columns.Add(controlProperties[CC.CONTROL_HIERARCHY_SELF_FK_COL] as string);
                            controlTabStruct.Columns.Add(displayColumns[0]);
                            controls.Add(new TreeControl(
                                controlTabStruct, PKColNames[0], controlProperties[CC.CONTROL_HIERARCHY_SELF_FK_COL] as string, 
                                displayColumns[0], UserAction.View));
                        }
                    }
                    break;
            }

            List<IField> fieldsAsInterface = new List<IField>(fields);
            List<IControl> controlsAsInterface = new List<IControl>(controls);

            Panel res =  new Panel(panelRow["table_name"] as string, panelId, 
                (int)panelRow["id_type"], panelRow["type_name"] as string, null,
                fieldsAsInterface, controlsAsInterface, PKColNames, null, viewProperties, controlProperties, parent);
            if(recursive) res.AddChildren(getPanelChildren(res, true));
            return res;
        }

        public IPanel getPanel(string tableName, bool recursive = true, IPanel parent = null)
        {
            int panelId = (int)fetchSingle("SELECT id_panel FROM panels WHERE table_name = '" + tableName 
                + "' AND id_project = ", CE.project.id);
            return getPanel(panelId, recursive, parent);
        }

        public IPanel getArchitectureInPanel() {        // make sure there is only one panel with id_parent = NULL
            int basePanelId = (int)fetchSingle("SELECT id_panel FROM panels WHERE id_parent IS NULL AND id_project = ", 
                CE.project.id);
            return getPanel(basePanelId, true);
        }

        private void RewritePanelProperties(IPanel panel) { 
            // ingore holder, except from that just copy
            panel.viewAttr.Remove("id_holder");
            Dictionary<string, object> insertVals = new Dictionary<string,object>();
            
            insertVals["id_panel"] = panel.panelId;
            insertVals["concerns"] = CC.ATTR_CONTROLS;
            foreach(string key in panel.controlAttr.Keys){
                insertVals["name"] = key;
                insertVals["val"] = panel.controlAttr[key].ToString();
                query("REPLACE INTO panel_meta ", insertVals);
            }

            insertVals["concerns"] = CC.ATTR_VIEW;
            foreach(string key in panel.viewAttr.Keys){
                insertVals["name"] = key;
                insertVals["val"] = panel.viewAttr[key].ToString();
                query("REPLACE INTO panel_meta ", insertVals);
            }
        }

        public void addPanel(IPanel panel, bool recursive = true)
        {
            Dictionary<string, object> insertVals = new Dictionary<string, object>();
            
            insertVals["id_project"] = CE.project.id;
            if(panel.viewAttr.ContainsKey("id_holder")){
                insertVals["id_holder"] = panel.viewAttr["id_holder"];
            }
            insertVals["id_type"] = panel.typeId;
            insertVals["type_name"] = panel.typeName;
            insertVals["table_name"] = panel.tableName;
            if(panel.parent != null)
                insertVals["id_parent"] = panel.parent;
            insertVals["pk_column_names"] = String.Join(",", panel.PKColNames);
            
            StartTransaction();
            panel.SetCreationId(NextId("panels"));
            query("INSERT INTO panels ", insertVals);
            CommitTransaction();

            RewritePanelProperties(panel);

            if (recursive) {
                foreach (IPanel child in panel.children)
                    addPanel(child, true);
            }
        }

        public void updatePanel(IPanel panel, bool recursive = true)
        {
            Dictionary<string, object> updateVals = new Dictionary<string, object>();

            if (panel.viewAttr.ContainsKey("id_holder"))
            {
                updateVals["id_holder"] = panel.viewAttr["id_holder"];
            }
            updateVals["id_type"] = panel.typeId;
            updateVals["type_name"] = panel.typeName;
            updateVals["table_name"] = panel.tableName;
            if (panel.parent != null)
                updateVals["id_parent"] = panel.parent;
            updateVals["pk_column_names"] = String.Join(",", panel.PKColNames);
            query("UPDATE panels SET", updateVals, "WHERE id_panel = ", panel.panelId);

            RewritePanelProperties(panel);
            if (recursive) {
                foreach (IPanel child in panel.children)
                    updatePanel(child, true);
            }
        }

        public void removePanel(IPanel panel)
        {
            query("DELETE FROM panels WHERE id_panel = ", panel.panelId);
        }


        private void AddField(IField field) {   // fieldId = 0
            int typeId = (int)fetchSingle("SELECT id_type FROM field_types WHERE type_name = '" + field.typeName + "'");
            Dictionary<string, object> insertVals = new Dictionary<string,object>();
            
            
            insertVals["id_panel"] = field.panelId;
            insertVals["id_type"] = typeId;     // TODO store type id?
            insertVals["column_name"] = field.column;
            
            StartTransaction();
            int fieldId = NextId("fields");
            field.SetCreationId(fieldId);    // must be 0 in creation
            query("INSERT INTO fields", insertVals);
            CommitTransaction();

            RewriteFieldProperties(field);
        }

        private void updateField(IField field){
            query("UPDATE fields SET id_type = ", field.typeId, ", column_name = ", field.column);
            RewriteFieldProperties(field);
        }

        private void RewriteFieldProperties(IField field){
            
            //set control properties
            Dictionary<string, object> insertVals = new Dictionary<string, object>();
            insertVals["id_field"] = field.fieldId;
            insertVals["concerns"] = CC.ATTR_CONTROLS; 
            
            IFK fk = null;
            IM2NMapping mapping = null;

            if(field is FKField){
                fk = (field as FKField).fk;
            }
            if(field is M2NMappingField){
                fk = mapping = (field as M2NMappingField).mapping;
            }

            if(fk != null){
                insertVals["name"] = CC.FIELD_REF_TABLE;
                insertVals["val"] = fk.refTable;
                query("REPLACE INTO fields_meta", insertVals);

                insertVals["name"] = CC.FIELD_REF_COLUMN;
                insertVals["val"] = fk.refColumn;
                query("REPLACE INTO fields_meta", insertVals);

                insertVals["name"] = CC.FIELD_DISPLAY_COLUMN;
                insertVals["val"] = fk.displayColumn;
                query("REPLACE INTO fields_meta", insertVals);
            }

            if(mapping != null){
                insertVals["name"] = CC.FIELD_REF_TABLE;
                insertVals["val"] = mapping.mapTable;
                query("REPLACE INTO fields_meta", insertVals);

                insertVals["name"] = CC.FIELD_MAP_MY_COLUMN;
                insertVals["val"] = mapping.myColumn;
                query("REPLACE INTO fields_meta", insertVals);

                insertVals["name"] = CC.FIELD_MAP_REF_COLUMN;
                insertVals["val"] = mapping.refColumn;
                query("REPLACE INTO fields_meta", insertVals);                
            }

            // validation rules & view properties - just copy
            insertVals = new Dictionary<string, object>();
            insertVals["id_field"] = field.fieldId;
            insertVals["concerns"] = "view";
            foreach(string attrKey in field.attr.Keys){
                insertVals["name"] = attrKey;
                insertVals["val"] = field.attr[attrKey].ToString();
                query("REPLACE INTO fields_meta", insertVals);
            }

            insertVals["concerns"] = "validation";
            foreach(string ruleKey in field.rules.Keys){
                insertVals["name"] = ruleKey;
                insertVals["val"] = field.rules[ruleKey].ToString();
                query("REPLACE INTO fields_meta", insertVals);
            }
        }
        

        public CE.User getUser(string userName, string password)
        {
            CE.User user = new CE.User();
            DataRow row = fetch("SELECT * FROM users WHERE login = '" + userName +"' AND MD5('" 
                + password + CC.SALT + "' = password");
            if(row == null) throw new Exception("User not found");
            user.id = (int)row["id_user"];
            user.login = userName;
            user.name = (string)row["name"];
            user.rights = (int)fetchSingle("SELECT access FROM access_rights WHERE id_user = ", 
                user.id, " AND id_project = ", CE.project.id);
            return user;
        }

        public CE.Project getProject(int projectId) { 
            CE.Project project = new CE.Project();
            DataRow row = fetch("SELECT * FROM project WHERE id_project = ", projectId);
            project.id = projectId;
            project.lastChange = (DateTime)row["last_modified"];
            project.name = (string)row["name"];
            project.serverName = (string)row["server_name"];
            project.connstringIS = (string)row["connstring_information_schema"];
            project.connstringWeb = (string)row["consstring_web"];
            return project;
        }

        public Dictionary<PanelTypes, int> PanelTypeNameIdMap() {
            Dictionary<PanelTypes, int> res = new Dictionary<PanelTypes, int>();
            DataTable tab = fetchAll("SELECT * FROM panel_types");
            foreach (DataRow row in tab.Rows) { 
                res.Add((PanelTypes)Enum.Parse(typeof(PanelTypes), row["type_name"] as string), (int)row["id_type"]);
            }
            return res;
        }

        public Dictionary<FieldTypes, int> FieldTypeNameIdMap()
        {
            Dictionary<FieldTypes, int> res = new Dictionary<FieldTypes, int>();
            DataTable tab = fetchAll("SELECT * FROM field_types");
            foreach (DataRow row in tab.Rows)
            {
                res.Add((FieldTypes)Enum.Parse(typeof(FieldTypes), row["type_name"] as string), (int)row["id_type"]);
            }
            return res;
        }
    }
}
