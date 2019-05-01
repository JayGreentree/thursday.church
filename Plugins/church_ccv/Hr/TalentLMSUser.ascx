<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TalentLMSUser.ascx.cs" Inherits="RockWeb.Plugins.church_ccv.Hr.TalentLMSUser" %>

<link rel="stylesheet" href="/Plugins/church_ccv/Hr/Styles/talent-lms.css">

<asp:Panel runat="server" ID="pnlTalentLMS" CssClass="talentlms">

    <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert alert-warning" />

    <asp:Panel runat="server" ID="pnlTalentLMSDashboard">
        
        <div class="talentlms-navigation">
            <h4 id="myCoursesToggle" class="navigation-item selected">My Courses</h4>
            <h4 id="allCoursesToggle" class="navigation-item">Available Courses</h4>
        </div>

        <div id="myCourses">

            <Rock:Grid runat="server" ID="gUserGrid">
                <Columns>
                    <Rock:RockBoundField DataField="Name" HeaderText="Name" HtmlEncode="false" />
                    <Rock:RockBoundField DataField="Progress" HeaderText="Progress" HtmlEncode="false" />
                    <Rock:RockBoundField DataField="Action" HtmlEncode="false" ItemStyle-CssClass="course-action" />
                </Columns>
            </Rock:Grid>
            
        </div>
        <div id="allCourses" class="hidden">

            <Rock:Grid runat="server" ID="gCourseGrid">
                <Columns>
                    <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                    <Rock:RockBoundField DataField="Action" HtmlEncode="false" ItemStyle-CssClass="course-action" />
                </Columns>
            </Rock:Grid>

        </div>

    </asp:Panel>

</asp:Panel>

<script>
    function pageLoad() {
        $('#myCoursesToggle').on('click', function () {
            showCoursesPanel('#myCourses');
            hideCoursesPanel('#allCourses');
        });

        $('#allCoursesToggle').on('click', function () {
            showCoursesPanel('#allCourses');
            hideCoursesPanel('#myCourses');
        });
    };

    function showCoursesPanel(panel) {
        $(panel).removeClass('hidden');
        $(panel + 'Toggle').addClass('selected');
    };

    function hideCoursesPanel(panel) {
        $(panel).addClass('hidden');
        $(panel + 'Toggle').removeClass('selected');
    };
</script>
