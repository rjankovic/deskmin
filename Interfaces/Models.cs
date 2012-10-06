using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using _min.Common;

namespace _min.Interfaces
{

    public interface ICondition
    {
        string Translate();
    }

    public interface IFK 
    {
        string refTable { get; }
        string myTable { get; }
        string myColumn { get; }
        string refColumn { get; }
        string displayColumn { get; set; }
        Dictionary<string, int> options { get; }    // display value & FK
        //Dictionary<object, object> getOptionsGeneral();   // maybe later
        bool validateInput(string inputValue);
    }

    public interface IM2NMapping : IFK {
        string mapTable { get; }
        // column in the mapping table that reffers to ref_table
        string mapRefColumn { get; }
        // column in the mapping table that reffers to my this fiel's column
        string mapMyColumn { get; }
        bool validateWholeInput(List<string> inputValues);
    }

    public interface IPanel        // no data included, will be added in the presenter 
    {
        DataRow PK { get; }
        int panelId { get; }
        string typeName { get; }
        int typeId { get; }
        string tableName { get; }
        IPanel parent { get; }
        List<string> PKColNames { get; }
        PropertyCollection viewAttr { get; }
        PropertyCollection controlAttr { get; }
        
        List<IPanel> children { get; }  // MDI
        List<IField> fields { get; }    // vratane dockov
        List<IControl> controls { get; }
        
        void AddChildren(List<IPanel> children);        // none of these must overwrite already existing object / property
        void AddFields(List<IField> fields);
        void AddControls(List<IControl> controls);
        void AddViewAttr(object key, object value);
        void AddControlAttr(object key, object value);
        
        void SetCreationId(int id);
        void SetParentPanel(IPanel parent);
    }

    public interface IField {
        //attrs readonly
        int fieldId { get; }   // just for initialization of a newly created field, probably to be removed later
        string column { get; }
        string typeName { get; }
        int panelId { get; set; }
        int typeId { get; }
        PropertyCollection attr { get; }
        PropertyCollection rules { get; }
        bool Validate(object value);
        bool ValidateSelf();
        object value { get; set; }

        void SetCreationId(int id);
    }

    public interface IFKField : IField {
        IFK fk { get; }
    }

    public interface IM2NMappingField : IField
    {
        IM2NMapping mapping { get; }
    }

    public interface IControl { 
    // type {UserAction}, attr - value; readonly
        DataTable data { get; set;  }
        List<string> PKColNames { get; } // action parameter
        UserAction action { get; }
    }

    public interface IBaseDriver 
    {
        // + constructor from connection
        // 3 connections total
        DataTable fetchAll(params object[] parts);
        DataRow fetch(params object[] parts);
        object fetchSingle(params object[] parts);
        int query(params object[] parts);   // returns rows affected
    }

    public interface IWebDriver : IBaseDriver     // webDB
    {

        void FillPanel(IPanel panel);
        int insertPanel(IPanel panel, DataRow values);  // returns insertedId
        void updatePanel(IPanel panel, DataRow values);
        void deletePanel(IPanel panel);   
    }

    public interface IStats : IBaseDriver {    // information_schema
        DataColumnCollection columnTypes(string tableName);
        List<IFK> foreignKeys(string tableName);
        List<List<string>> indexes(string tableName);
        List<string> primaryKeyCols(string tableName);
        List<string> TwoColumnTables();
        List<string> TableList();
        List<IM2NMapping> findMappings();
        DateTime TableCreation(string tableName);//...
    }

    public interface IArchitect  // systemDB, does not fill structures with data
    {
        IPanel getArchitectureInPanel();        // hierarchia - vnutorne vola getPanel

        IPanel propose();   // for the whole site
        IPanel proposeForTable(string tableName);
        bool checkPanelProposal(IPanel proposal, bool recursive = true);
        bool checkPanelProposal(int panelId, bool recursive = true);    // load from db
        bool checkProposal();       // for the whole project, load from db
    }

    public interface ISystemDriver : IBaseDriver // systemDB
    {
        void saveLog();
        void logUserAction(DataRow data);
        bool isUserAuthorized(int panelId, UserAction act);
        void doRequests();
        IPanel getPanel(string tableName, UserAction action, bool recursive = true, IPanel parent = null);
        IPanel getPanel(int panelId, bool recursive = true, IPanel parent = null);
        IPanel getArchitectureInPanel();
        void addPanel(IPanel panel, bool recursive = true);
        void updatePanel(IPanel panel, bool recursive = true);
        void removePanel(IPanel panel);
        
        Common.Environment.User getUser(string userName, string password);
        Common.Environment.Project getProject(int projectId);
        
        Dictionary<PanelTypes, int> PanelTypeNameIdMap();
        Dictionary<FieldTypes, int> FieldTypeNameIdMap();
        
        bool ProposalExists();
        DataTable fetchBaseNavControlTable();   // the DataTable for main TreeControl / MenuDrop
    }
}
