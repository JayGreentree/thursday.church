﻿// <copyright>
// Copyright by Central Christian Church
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Security;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_centralaz.Widgets
{
    [DisplayName( "Login Status Lava" )]
    [Category( "com_centralaz > Widgets" )]
    [Description( "A lava block for login." )]
    [LinkedPage( "My Account Page", "The page used to view an account" )]
    [CodeEditorField( "Lava Template", "Lava template to use to display the login status.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"{% include '~/Plugins/com_centralaz/Widgets/Lava/LoginStatusLava.lava' %}", "", 2 )]
    [BooleanField( "Enable Debug", "Display a list of merge fields available for lava.", false, "", 3 )]
    public partial class LoginStatusLava : RockBlock
    {

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            RouteAction();
        }

        #endregion

        #region Control Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            LoadContent();
        }

        #endregion

        #region Internal Methods
        // route the request to the correct panel
        private void RouteAction()
        {
            var sm = ScriptManager.GetCurrent( Page );

            if ( Request.Form["__EVENTARGUMENT"] != null )
            {

                string[] eventArgs = Request.Form["__EVENTARGUMENT"].Split( '^' );

                if ( eventArgs.Length == 2 )
                {
                    string action = eventArgs[0];
                    string parameters = eventArgs[1];

                    int argument = 0;
                    int.TryParse( parameters, out argument );

                    switch ( action )
                    {
                        case "UserLoginLogout":
                            UserLoginLogout();
                            break;
                    }
                }
            }
            else
            {
                LoadContent();
            }
        }

        private void UserLoginLogout()
        {
            if ( CurrentUser == null )
            {
                var site = RockPage.Layout.Site;
                if ( site.LoginPageId.HasValue )
                {
                    site.RedirectToLoginPage( true );
                }
                else
                {
                    FormsAuthentication.RedirectToLoginPage();
                }
            }
            else
            {

                var transaction = new Rock.Transactions.UserLastActivityTransaction();
                transaction.UserId = CurrentUser.Id;
                transaction.LastActivityDate = RockDateTime.Now;
                transaction.IsOnLine = false;
                Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );


                FormsAuthentication.SignOut();

                // After logging out check to see if an anonymous user is allowed to view the current page.  If so
                // redirect back to the current page, otherwise redirect to the site's default page
                var currentPage = Rock.Web.Cache.PageCache.Read( RockPage.PageId );
                if ( currentPage != null && currentPage.IsAuthorized( Authorization.VIEW, null ) )
                {
                    Response.Redirect( CurrentPageReference.BuildUrl() );
                    Context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    RockPage.Layout.Site.RedirectToDefaultPage();
                }
            }
        }

        public void LoadContent()
        {
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

            // add linked pages
            Dictionary<string, object> linkedPages = new Dictionary<string, object>();
            linkedPages.Add( "MyAccountPage", LinkedPageUrl( "MyAccountPage", null ) );
            mergeFields.Add( "LinkedPages", linkedPages );
            mergeFields.Add( "CurrentUser", CurrentUser );

            lOutput.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields );

            // show debug info
            if ( GetAttributeValue( "EnableDebug" ).AsBoolean() && IsUserAuthorized( Authorization.EDIT ) )
            {
                lDebug.Visible = true;
                lDebug.Text = mergeFields.lavaDebugInfo();
            }
        }

        #endregion

    }
}