using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using _min.Common;

namespace _min.Interfaces
{

    interface ICondition
    {
        string Translate();
    }

    interface IFK 
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

    interface IM2NMapping : IFK {
        string mapTable { get; }
        // column in the mapping table that reffers to ref_table
        string mapRefColumn { get; }
        // column in the mapping table that reffers to my this fiel's column
        string mapMyColumn { get; }
        bool validateWholeInput(List<string> inputValues);
    }

    interface IPanel        // bez dat, tie pripajat vo view (?) 
    {
        /*
         Dictionary<string, object> attr;
         Dictionary<string, object> state; (?)
         Panel[] children;
         int Dock ( = parent field_id)
         */
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
        
        void AddChildren(List<IPanel> children);
        void SetCreationId(int id);
    }

    interface IField {
        //attrs readonly
        int fieldId { get; }   // just for initialization of a newly created field, probably to be removed later
        string column { get; }
        string typeName { get; }
        int panelId { get; }
        int typeId { get; }
        PropertyCollection attr { get; }
        PropertyCollection rules { get; }
        bool Validate(object value);
        bool ValidateSelf();
        object value { get; set; }

        void SetCreationId(int id);
    }

    interface IControl { 
    // type {UserAction}, attr - value; readonly
        DataTable data { get; set;  }
        List<string> PKColNames { get; } // action parameter
        UserAction action { get; }
    }

    interface IBaseDriver 
    {
        // + constructor from connection
        // 3 connections total
        DataTable fetchAll(params object[] parts);
        DataRow fetch(params object[] parts);
        object fetchSingle(params object[] parts);
        int query(params object[] parts);   // returns rows affected
    }

    interface IWebDriver : IBaseDriver     // webDB
    {

        void FillPanel(IPanel panel);
        int insertPanel(IPanel panel, DataRow values);  // returns insertedId
        void updatePanel(IPanel panel, DataRow values);
        void deletePanel(IPanel panel);
        // ICondition - rozsah/zhoda retazca/cisla (Match)
        //DataTable getPanelList(int idPanel, ICondition[] conditions = null, 
        //    DataColumn[] orderBy = null, int limit = 0);   
    }

    interface IStats : IBaseDriver {    // information_schema
        DataColumnCollection columnTypes(string tableName);
        List<IFK> foreignKeys(string tableName);
        List<List<string>> indexes(string tableName);
        List<string> primaryKeyCols(string tableName);
        List<string> TwoColumnTables();
        List<string> TableList();
        List<IM2NMapping> findMappings();
        DateTime TableCreation(string tableName);
        //...
    }

    interface IArchitect  // systemDB, does not fill structures with data
    {
        DataSet getArchitecture();
        IPanel getArchitectureInPanel();        // hierarchia - vnutorne vola getPanel

        IPanel propose();   // for the whole site
        IPanel proposeForTable(string tableName);
        bool checkPanelProposal(IPanel proposal, bool recursive = true);
        bool checkPanelProposal(int panelId, bool recursive = true);
        //...
    }

    interface ISystemDriver : IBaseDriver // systemDB
    {
        void saveLog();
        void logUserAction(DataRow data);
        bool isUserAuthorized(int panelId, UserAction act);
        void doRequests();
        IPanel getPanel(string tableName, bool recursive = true, IPanel parent = null);
        IPanel getPanel(int panelId, bool recursive = true, IPanel parent = null);
        IPanel getArchitectureInPanel();
        void addPanel(IPanel panel, bool recursive = true);
        void updatePanel(IPanel panel, bool recursive = true);
        void removePanel(IPanel panel);
        
        Common.Environment.User getUser(string userName, string password);
        Common.Environment.Project getProject(int projectId);
        
        public Dictionary<PanelTypes, int> PanelTypeNameIdMap();
        public Dictionary<FieldTypes, int> FieldTypeNameIdMap();
    }
}
