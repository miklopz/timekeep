<%@ Page Language="C#" AutoEventWireup="true" %>

<!DOCTYPE html>

<html>
    <head runat="server">
        <title>Time Keep Application</title>
        <%
            Response.Write("<link type=\"text/css\" href=\"main.min.css?v=" + TimeKeep.Web.UI.Configuration.Cache.CSSVersion + "\" rel=\"stylesheet\" />");
            Response.Write("<script type=\"text/javascript\">var initPara = {endpoint:'" + TimeKeep.Web.UI.Configuration.API.Endpoint + "'," +
                "user:'" + (HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated ? "anonymous" : HttpContext.Current.User.Identity.Name.ToLowerInvariant().Replace(" ", "")) +
                "',accessToken:" + (HttpContext.Current.Request.Cookies["OAuthIDToken"] != null ? string.Concat("'", TimeKeep.Web.UI.OAuthModule.TokenCache.GetToken(Guid.Parse(HttpContext.Current.Request.Cookies["OAuthIDToken"].Value)).AccessToken, "'") : "null") +
		",apiVersion:'" + TimeKeep.Web.UI.Configuration.API.ApiVersion + "'" +
                "};</script>");
            Response.Write("<script type=\"text/javascript\" src=\"main.min.js?v=" + TimeKeep.Web.UI.Configuration.Cache.JSVersion + "\"></script>");
        %>
    </head>
    <body>
        <header>
            <h1>Time Keep Application</h1>
            <p>Welcome <span id="username"><%= (HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated ? "Anonymous" : HttpContext.Current.User.Identity.Name) %></span></p>
            <p>This application can be used to keep track of your scorecard and non-scorecard utilization. The current time is <span id="currentTime"></span>.</p>
        </header>
        <section id="mainloading">
            Loading...
        </section>
        <section id="noentries" style="display:none">
            You have not began to log your time today or you have deleted all of today's time.<br />
            <button id="btnSYD" onclick="TimeKeep.btnStartYourDate();">Start your day</button>
        </section>
        <section id="entries" style="display:none">
            <table id="tblEntries">
                <thead>
                    <tr>
                        <th>Start Time</th>
                        <th>End Time</th>
                        <th>Labor Type</th>
                        <th>Case Number</th>
                        <th>Labor</th>
                        <th>Logged?</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody id="tbEntries"></tbody>
                <tfoot>
                    <tr>
                        <td id="tdtotals" colspan="7"></td>
                    </tr>
                </tfoot>
            </table>
            <div id="divCaseSummary" style="display:none">
                Log entries for this case:
                <table>
                    <thead>
                        <tr>
                            <th>Start Time</th>
                            <th>End Time</th>
                            <th>Labor Type</th>
                            <th>Labor</th>
                        </tr>
                    </thead>
                    <tbody id="tbodySummary">

                    </tbody>
                    <tfoot>
                        <tr>
                            <td colspan="4">
                                Total labor time for this case today is <span id="spanTotalLabor"></span>. <span id="spanTotalUnloggedLaborText"><br />However, you have not logged <span id="spanTotalUnloggedLabor"></span> yet.</span>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="4">
                                <button id="btnLogAndDetailAll" onclick="TimeKeep.btnLogAndDetailAllClick();">Log all entries</button>
                                <button id="btnCloseSummary" onclick="TimeKeep.btnCloseSummaryClick();">Close</button>
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </div>
            <br class="break" />
        </section>
        <footer>
            © <%= DateTime.Now.Year %> Michael Lopez
        </footer>
        <div class="popup" id="popup" style="display:none">
            <div class="wrapper">
                <div class="shadow"></div>
                <div class="popup-window">
                    <div class="header" id="popupheader">
                        This is the header
                    </div>
                    <div class="body">
                        <div class="errors" id="errors" style="display:none">
                            The following errors were found
                            <ul id="errorList">

                            </ul>
                        </div>
                        <div id="caption"></div>
                        <br />
                        <div class="form">
                            <div class="row">
                                <label for="StartTimeHH">Start Time</label>
                                <select id="StartTimeHH" name="StartTimeHH">
                                    <%
                                        for(int i = 1; i <= 12; i++)
                                        {
                                            Response.Write("<option value=\"");
                                            Response.Write(i);
                                            Response.Write("\">");
                                            if (i < 10)
                                                Response.Write("0");
                                            Response.Write(i);
                                            Response.Write("</option>");
                                        }
                                    %>
                                </select> :
                                <select id="StartTimeMM" name="StartTimeMM">
                                    <%
                                        for(int i = 0; i <= 59; i++)
                                        {
                                            Response.Write("<option value=\"");
                                            Response.Write(i);
                                            Response.Write("\">");
                                            if (i < 10)
                                                Response.Write("0");
                                            Response.Write(i);
                                            Response.Write("</option>");
                                        }
                                    %>
                                </select> : 
                                <select id="StartTimeSS" name="StartTimeSS">
                                    <%
                                        for(int i = 0; i <= 59; i++)
                                        {
                                            Response.Write("<option value=\"");
                                            Response.Write(i);
                                            Response.Write("\">");
                                            if (i < 10)
                                                Response.Write("0");
                                            Response.Write(i);
                                            Response.Write("</option>");
                                        }
                                    %>
                                </select>
                                <select id="StartTimeAM" name="StartTimeAM">
                                    <option value="AM">AM</option>
                                    <option value="PM">PM</option>
                                </select>
                            </div>
                            <div class="row">
                                <label for="EndTimeHH">End Time</label>
                                <select id="EndTimeHH" name="EndTimeHH">
                                    <%
                                        for(int i = 1; i <= 12; i++)
                                        {
                                            Response.Write("<option value=\"");
                                            Response.Write(i);
                                            Response.Write("\">");
                                            if (i < 10)
                                                Response.Write("0");
                                            Response.Write(i);
                                            Response.Write("</option>");
                                        }
                                    %>
                                </select> :
                                <select id="EndTimeMM" name="EndTimeMM">
                                    <%
                                        for(int i = 0; i <= 59; i++)
                                        {
                                            Response.Write("<option value=\"");
                                            Response.Write(i);
                                            Response.Write("\">");
                                            if (i < 10)
                                                Response.Write("0");
                                            Response.Write(i);
                                            Response.Write("</option>");
                                        }
                                    %>
                                </select> : 
                                <select id="EndTimeSS" name="EndTimeSS">
                                    <%
                                        for(int i = 0; i <= 59; i++)
                                        {
                                            Response.Write("<option value=\"");
                                            Response.Write(i);
                                            Response.Write("\">");
                                            if (i < 10)
                                                Response.Write("0");
                                            Response.Write(i);
                                            Response.Write("</option>");
                                        }
                                    %>
                                </select>
                                <select id="EndTimeAM" name="EndTimeAM">
                                    <option value="AM">AM</option>
                                    <option value="PM">PM</option>
                                </select>
                            </div>
                            <div class="row">
                                <label for="Category">Labor Type</label>
                                <select id="Category" name="Category" onchange="TimeKeep.megaUpdate()" >
                                    <optgroup id="CategoryScorecard" label="Scorecard Labor"></optgroup>
                                    <optgroup id="CategoryNonscorecard" label="Non-scorecard Labor"></optgroup>
                                    <optgroup id="CategoryOut" label="Time Away"></optgroup>
                                </select>
                            </div>
                            <div class="row">
                                <label for="CaseNumber">Case Number</label>
                                <input type="number" id="CaseNumber" name="CaseNumber" />
                            </div>
                            <br />
                        </div>
                    </div>
                    <div class="footer">
                        <button id="btnSave" onclick="TimeKeep.btnSaveClick()">Save</button>
                        <button id="btnCancel" onclick="TimeKeep.btnCancelClick()">Cancel</button>
                    </div>
                </div>
            </div>
        </div>
        
        <script type="text/javascript">TimeKeep.init();</script>
    </body>
</html>