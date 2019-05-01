<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Staff.ascx.cs" Inherits="RockWeb.Plugins.org_newpointe.Staff.Staff" %>

<div class="container-fluid staff">
   <h3><span><asp:Label runat="server" ID="lblGroupName"></asp:Label></span></h2> 

    <div class="row">
	<div class="leaders">
        <asp:Repeater runat="server" ID="rptStaff" OnItemDataBound="rptStaff_ItemDataBound">
            <ItemTemplate>
            <div class="leader">
                    <asp:Image runat="server" CssClass="img-circle" ImageUrl='<%# Eval("PhotoUrl") %>' />
                    <h4 class="margin-b-none"><asp:Label runat="server" Text='<%# Eval("Name") %>' /></h4>
                    <span class="text-muted"><asp:Label runat="server" Text='<%# Eval("Position") %>' /></span>
                </div>
            </ItemTemplate>
        </asp:Repeater>
    </div>
</div>