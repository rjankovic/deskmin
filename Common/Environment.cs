using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace _min.Common
{
    public enum DbServer { MySql, MSSQL };
    public enum UserAction { View, Insert, Update, Delete }
    public enum AppRequest { ArchitectureReload, StopLogging, StartLogging }
    public enum PanelTypes { Editable, NavTable, NavTree, MenuDrop, MenuTabs, Monitor, Container }
    public enum FieldTypes { FK, M2NMapping, Date, DateTime, Time, Holder, Varchar, Text, Decimal, Ordinal, Bool }

    public static class Environment
    {
        // User and Project objects can be created freely, 
        // but only once initiated in the Environment
        public class User {
            public int id; 
            public string name;
            public string login;
            public  int rights;
        }

        public class Project {
            public int id; 
            public string name;
            public string serverName;
            public string serverVersion;
            public DateTime lastChange;
            public string connstringWeb;
            public string connstringIS;
        }

        private static User _user;

        public static User user {
            get { 
                return _user; 
            }
            set {
                if (_user == null)
                    _user = value;
                else
                    throw new ReadOnlyException("User already authenticated");
            }
        }

        public static Project _project;

        public static Project project
        {
            get 
            {
                return _project;
            }
            set
            {
                if (_project == null)
                    _project = value;
                else
                    throw new ReadOnlyException("Project already initialized");
            }
        }
    }

    static class Constants {
        public const string SALT = "hjjh5435435jl43kj5ljlj53l4j5lk4";

        public const string CONTROL_DISABLED = "disabled";      // this panel may not be displayed / edited / ...

        // depreceated
        public const string PANEL_EDITABLE = "Editable";    // from panel_types
        public const string PANEL_NAVTABLE = "NavTable";
        public const string PANEL_NAVTREE = "NavTree";
        public const string PANEL_MENUDROP = "MenuDrop";
        public const string PANEL_MENUTABS = "MenuTabs";
        public const string PANEL_MONITOR = "Monitor";
        public const string PANEL_CONTAINER = "Container";  // no data to display (shoul by a panelType? probably)
        // end depreceated

        public const string PANEL_NAME = "panelName";

        public const string FIELD_REF_TABLE = "refTable";   // FK
        public const string FIELD_REF_COLUMN = "refColumn";
        public const string FIELD_DISPLAY_COLUMN = "displayColumn";

        public const string FIELD_MAP_TABLE = "mapMyTable";     // N2MMapping
        public const string FIELD_MAP_MY_COLUMN = "mapMyColumn";
        public const string FIELD_MAP_REF_COLUMN = "mapRefColumn";

        public const string COLUMN_EDITABLE = "editable";
        public const string FIELD_DATE_ONLY = "dateOnly";
        public const string FIELD_POSITION = "position";

        public const string RULES_REQUIRED = "required";
        public const string RULES_ZIP = "zip";
        public const string RULES_ORDINAL = "ordinal";
        public const string RULUES_DECIMAL = "decimal";
        public const string RULES_DATETIME = "datetime";
        public const string RULES_DATE = "date";

        public const string CONTROL_HIERARCHY_SELF_FK_COL = "hierarchySelfFKColumn";
        
        public const string NAVTAB_COLUMNS_DISLAYED = "NavTabColumnsDisplayed"; // !INTEGER
        public const int NAVTAB_COLUMNS_DISLAYED_DEFAULT = 4;

        // Nav = {NavTab, NavTree}
        public const string NAV_INSERT_CAPTION = "NavInsertCaption";       // button caption above NavTab / Tree
        public const string NAV_DELETE_CAPTION = "NavDeleteCaption";
        public const string NAV_UPDATE_CAPTION = "NavUpdateCaption";       // terminology: Update == Edit
        public const string NAV_PANEL_ID = "NavPanelId";           // !INTEGER the panel which will be displayed after firing the control
        // !BOOL PKCol(s) are PK in the table edited, must have NAV_ID_PANEL set,
        // this is the default option if navThroughPanels is not specified
        public const string NAV_THROUGH_RECORDS = "NavThroughRecords";
        // !BOOL control PKCol is the panel id, (must be a single int),
        // excludes NAV_THROUGH_RECORDS, the containing panel must not have a table
        public const string NAV_THROUGH_PANELS = "NavThroughPanels";        
        public const string NAVTREE_INSERT_CHILD = "NavtreeInsertChildCaption"; // "Insert a child option somewhere near the tree node"

        public const string ATTR_CONTROLS = "controls";     // ENUM from DB
        public const string ATTR_VALIDATION = "validation";
        public const string ATTR_VIEW = "view";

        public const string SERVER_MYSQL = "MySql";     // code in projects.server_type
        public const string SERVER_MSSQL = "MSSql";

        
        
        /// <summary>
        /// control properties for panels have string keys 
        /// UserAction.ToString() => control caption and -||- + REQUIRED_SUFFIX => int
        /// see SystemDriver(s)
        /// </summary>
        public const string CONTROL_ACCESS_LEVEL_REQUIRED_SUFFIX = "ALR";

        public const string PANEL_DISPLAY_COLUMN_ORDER = "displayOrder";
        

    }

}
