<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SlackAuth.ascx.cs" Inherits="com.bricksandmortarstudio.Slack.OAuthLogin" %>

    <asp:Panel ID="pnlLogin" runat="server">
            <div class="row">
                <div id="divAuth" runat="server" class="col-sm-12">
                        <asp:Literal ID="lPromptMessage" runat="server" />
                        <asp:Literal ID="lPreviouslyCompletedMessage" runat="server" />
                        <asp:LinkButton Id="lbSlackAuth" runat="server" Text="Link Your Account to Slack" CssClass="btn btn-authenication" OnClick="lbSlackAuth_Click" CausesValidation="false"></asp:LinkButton>
                        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="alert alert-warning block-message margin-t-md"/>
                </div>
            </div>    
    </asp:Panel>

    
    <asp:Panel ID="pnlLockedOut" runat="server" Visible="false">

        <div class="alert alert-danger">
            <asp:Literal ID="lLockedOutCaption" runat="server" />
        </div>

    </asp:Panel>

    <asp:Panel ID="pnlConfirmation" runat="server" Visible="false">

        <div class="alert alert-warning">
            <asp:Literal ID="lConfirmCaption" runat="server" />
        </div>

    </asp:Panel>
   

