using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using _min.Interfaces;
using _min.Common;

namespace _min.Interfaces
{
    interface Presenter
    {
        void panelSubmitted(IPanel panel, UserAction action, DataTable data);
        void navigationMove(IPanel panel, UserAction action);
        void proposalSubmitted(IPanel panel);
        void Validate(IPanel panel, DataTable data);
        //...
    }
}
