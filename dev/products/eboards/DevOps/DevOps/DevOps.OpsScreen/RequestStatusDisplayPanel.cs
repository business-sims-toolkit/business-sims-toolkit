using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CoreUtils;

using DevOps.OpsEngine;

namespace DevOps.OpsScreen
{
	internal class RequestStatusDisplayPanel:FlowLayoutPanel
    {
	    int heightPadding = 5;
	    int widthPadding = 5;
        public int HeightPadding
        {
            get { return heightPadding; }
        }

        public int WidthPadding
        {
            get { return widthPadding; }
        }

	    int rowHeight;

	    string mbu;
	    RequestsManager requestsManager;
	    DemandsManager demandsManager;

	    List<RequestStatusDisplayRow> requestsList;

        Dictionary<string, RequestStatusDisplayRow> requestsNameToDisplayRow;

        int maxStatusRows = SkinningDefs.TheInstance.GetIntData("max_status_rows", 7);

        public RequestStatusDisplayPanel(RequestsManager rm,DemandsManager dm,string mbuSelected, int height, int width)
        {
            requestsManager = rm;
            //requestsManager.NewServiceStatusReceived += NewServiceDeveloping;
            demandsManager = dm;
            //demandsManager.DemandStatusReceived += DemandDeveloping;

            
            Height = height;
            Width = width;
            requestsList = new List<RequestStatusDisplayRow>();
            requestsNameToDisplayRow = new Dictionary<string, RequestStatusDisplayRow>();
            mbu=mbuSelected;
            rowHeight = 20;

            BackColor = Color.Black;
        }

	    void NewServiceDeveloping(string mbuSelected, string serviceName, string status, bool isHidden)
        {
            //if (IsServiceExist(serviceName)  && mbu.Equals(mbuSelected))#
            if (requestsNameToDisplayRow.ContainsKey(mbuSelected + " " + serviceName) && mbu.Equals(mbuSelected))
            {
                //just update the status
                RequestStatusDisplayRow newServiceStatusRow =
                    requestsNameToDisplayRow[mbuSelected + " " + serviceName];//GetService(serviceName);
                if (newServiceStatusRow != null)
                {
                    if (isHidden)
                    {
                        newServiceStatusRow.Dispose();
                    }
                    else if (newServiceStatusRow.IsStatusChanged(status))
                    {
                        newServiceStatusRow.UpdateStatus(status);
                    }
                }
            }
            else if(mbu.Equals(mbuSelected))
            {
                if (requestsList.Count < maxStatusRows)
                {
                    AddNewRow(mbuSelected, serviceName, string.Empty, status);
                }
            }
        }

	    void DemandDeveloping(string mbuSelected, string serviceName, string demandId, string status, bool isHidden)
        {
            //IsServiceExist(serviceName, demandId) && mbu.Equals(mbuSelected))
            if (requestsNameToDisplayRow.ContainsKey(mbuSelected + " " + serviceName) && mbu.Equals(mbuSelected))
            {
                //just update the status
                RequestStatusDisplayRow demandStatusDisplayRow =
                    requestsNameToDisplayRow[mbuSelected + " " + serviceName];
                    //GetService(serviceName,demandId);
                if (demandStatusDisplayRow != null)
                {
                    if (isHidden)
                    {
                        //requestsList.Remove(demandStatusDisplayRow);
                        requestsNameToDisplayRow.Remove(mbuSelected + " " + serviceName);
                        demandStatusDisplayRow.Dispose();
                        Controls.Remove(demandStatusDisplayRow);
                        demandStatusDisplayRow = null;
                    }
                    else if (demandStatusDisplayRow.IsStatusChanged(status))
                    {
                        demandStatusDisplayRow.UpdateStatus(status);
                    }
                }
            }
            else if (mbu.Equals(mbuSelected))
            {
                if (requestsList.Count < maxStatusRows)
                {
                    if (isHidden)
                    {
                        return;
                    }
                    AddNewRow(mbuSelected, serviceName, demandId, status);
                }
               
            }
        }

        void AddNewRow(string mbuSelected, string serviceName, string demandId, string status)
        {
            RequestStatusDisplayRow statusDisplayRow = new RequestStatusDisplayRow(mbuSelected, serviceName, demandId,
                rowHeight, Width);
            Controls.Add(statusDisplayRow);
            statusDisplayRow.FlowDirection = FlowDirection.LeftToRight;
            statusDisplayRow.WrapContents = false;
            statusDisplayRow.UpdateStatus(status);
            
            
            requestsNameToDisplayRow.Add(mbuSelected + " " + serviceName, statusDisplayRow);
           // requestsList.Add(statusDisplayRow);
        }

	    bool IsServiceExist(string serviceName, string demandId = "")
        {
            foreach (RequestStatusDisplayRow requestStatusDisplayRow in requestsList)
            {
                if (requestStatusDisplayRow.NewServiceName.Equals(serviceName))
                {
                    if (requestStatusDisplayRow.DemandId.Equals(demandId) || demandId ==string.Empty)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

	    RequestStatusDisplayRow GetService(string serviceName, string demandId = "")
        {
            foreach (RequestStatusDisplayRow requestStatusDisplayRow in requestsList)
            {
                if (requestStatusDisplayRow.NewServiceName.Equals(serviceName))
                {
                    if (requestStatusDisplayRow.DemandId.Equals(demandId) || demandId ==string.Empty)
                    {
                        return requestStatusDisplayRow;
                    }
                }
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                requestsList.Clear();
                //requestsManager.NewServiceStatusReceived -= NewServiceDeveloping;
                requestsManager.Dispose();

                //demandsManager.DemandStatusReceived -= DemandDeveloping;
                demandsManager.Dispose();
            }
        }
    }
}
