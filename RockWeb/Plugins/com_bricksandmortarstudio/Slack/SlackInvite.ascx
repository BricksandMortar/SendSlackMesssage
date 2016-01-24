<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SlackInvite.ascx.cs" Inherits="RockWeb.Plugins.com_bricksAndMortarStudio.Cms.SlackInvite" %>
<%@ Register TagPrefix="Rock" Namespace="Rock.Web.UI.Controls" Assembly="Rock" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:RockTextBox ID="txtEmail" runat="server" CssClass="margin-b-sm" Placeholder="you@yourdomain.com" />
        <asp:ValidationSummary ID="vsDetails" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
        <asp:RegularExpressionValidator ID="regexEmailValid" runat="server" Display="None" ValidationExpression="\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" ControlToValidate="txtEmail" ErrorMessage="Please provide a valid email address."></asp:RegularExpressionValidator>
        <Rock:BootstrapButton ID="btnInvite" runat="server" CssClass="btn btn-primary btn-block" OnClick="btnInvite_Click">Get My Invite</Rock:BootstrapButton>
        
        <asp:Literal ID="lMessages" runat="server" />

    </ContentTemplate>
</asp:UpdatePanel>

