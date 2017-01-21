<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EditEntry.ascx.cs" Inherits="RockWeb.Plugins.com_centralaz.Prayerbook.EditEntry" %>

<asp:HiddenField runat="server" ID="hidEntryId" Visible="false" />
<asp:HiddenField runat="server" ID="hidBookId" Visible="false" />

<asp:UpdatePanel runat="server" ID="upnlContent">
    <ContentTemplate>
        <asp:ValidationSummary ID="ValidationSummary1" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
        <div class="col-md-6">
            <div class="row">
                <div class="col-md-6">
                    <Rock:RockLiteral ID="lName" runat="server" Label="Name"></Rock:RockLiteral>
                    <Rock:RockDropDownList ID="ddlContributors" runat="server" Label="Name" DataValueField="Id" DataTextField="FullName" AutoPostBack="true" CausesValidation="false" SourceTypeName="Rock.Model.Person, Rock" OnSelectedIndexChanged="ddlContributors_SelectedIndexChanged" Required="true" />
                </div>
                <div class="col-md-6">
                    <Rock:RockLiteral ID="lSpouseName" runat="server" Label="Spouse"></Rock:RockLiteral>
                </div>
            </div>
            <div class="row">
                <div class="col-md-6">
                    <Rock:RockDropDownList ID="ddlMinistry" runat="server" Label="Ministry" DataValueField="Id" DataTextField="Value" CausesValidation="false" SourceTypeName="Rock.Model.DefinedValue, Rock" />
                </div>
                <div class="col-md-6">
                    <Rock:RockDropDownList ID="ddlSubministry" runat="server" Label="Subministry" DataValueField="Id" DataTextField="Value" CausesValidation="false" SourceTypeName="Rock.Model.DefinedValue, Rock" />
                </div>
            </div>
            <Rock:RockTextBox ID="dtbPraise1" runat="server" CausesValidation="false" Rows="4" TextMode="MultiLine" />
            <Rock:RockTextBox ID="dtbMinistryNeed1" runat="server" CausesValidation="false" Rows="4" TextMode="MultiLine" />
            <Rock:RockTextBox ID="dtbMinistryNeed2" runat="server" CausesValidation="false" Rows="4" TextMode="MultiLine" />
            <Rock:RockTextBox ID="dtbMinistryNeed3" runat="server" CausesValidation="false" Rows="4" TextMode="MultiLine" />
            <Rock:RockTextBox ID="dtbPersonalRequest1" runat="server" CausesValidation="false" Rows="4" TextMode="MultiLine" />
            <Rock:RockTextBox ID="dtbPersonalRequest2" runat="server" CausesValidation="false" Rows="4" TextMode="MultiLine" />

            <div class="container-fluid">
                <div class="row">
                    <div class="col-md-2">
                        <Rock:BootstrapButton ID="btnSave" runat="server" Text="Save Entry" OnClick="btnSave_Click" CssClass="btn btn-primary" />
                    </div>
                    <div class="col-md-2">
                        <Rock:BootstrapButton ID="btnDelete" runat="server" Text="Delete Entry" OnClick="btnDelete_Click" CssClass="btn btn-default" CausesValidation="false" />
                    </div>
                    <div class="col-md-2">
                        <asp:LinkButton ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="btn btn-link" CausesValidation="false" />
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-6">
            <Rock:Grid ID="gHistory" runat="server" EmptyDataText="No History"
                ShowActionRow="false" OnRowSelected="gHistory_RowSelected">
                <Columns>
                    <Rock:RockBoundField DataField="CreatedDateTime" HeaderText="Modified on" DataFormatString="{0:d}" />
                    <Rock:RockBoundField DataField="CreatedByPerson" HeaderText="By" />
                </Columns>
            </Rock:Grid>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
