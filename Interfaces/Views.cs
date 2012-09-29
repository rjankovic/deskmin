using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace _min.Interfaces
{
    interface IView // = Form
    {
        // constructor - IView(IPanel)
        void fill(DataTable data);
        void clear();
        DataTable collectInput();
        void addChild(IView view, int panelId);
        void onSubmit(object sender, EventArgs e);
        void onNavigation(object sender, EventArgs e);
        void Show();
    }
}
