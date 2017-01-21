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
using System.Web.UI;

using com.centralaz.Accountability.Data;
using com.centralaz.Accountability.Model;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_centralaz.Accountability
{
    /// <summary>
    /// Lists all the Accountability Group Type Questions.
    /// </summary>
    [DisplayName( "Accountability Question List" )]
    [Category( "com_centralaz > Accountability" )]
    [Description( "Lists all the questions for the Accountability Group Type." )]

    public partial class AccountabilityQuestionList : Rock.Web.UI.RockBlock, Rock.Web.UI.ISecondaryBlock
    {
        #region Fields

        private GroupType _accountabilityGroupType = null;

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            bool canEdit = IsUserAuthorized( Rock.Security.Authorization.EDIT );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.AddConfigurationUpdateTrigger( upnlContent );

            gAccountabilityQuestions.RowItemText = "Questions";
            gAccountabilityQuestions.DataKeyNames = new string[] { "id" };
            gAccountabilityQuestions.Actions.ShowAdd = canEdit;
            gAccountabilityQuestions.IsDeleteEnabled = canEdit;
            gAccountabilityQuestions.Actions.AddClick += gAccountabilityQuestions_Add;
            gAccountabilityQuestions.GridRebind += gAccountabilityQuestions_GridRebind;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Add event of the gAccountabilityQuestions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gAccountabilityQuestions_Add( object sender, EventArgs e )
        {
            ShowModal( 0 );
        }

        /// <summary>
        /// Handles the Edit event of the gAccountabilityQuestions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gAccountabilityQuestions_Edit( object sender, RowEventArgs e )
        {
            ShowModal( e.RowKeyId );
        }

        /// <summary>
        /// Handles the Delete event of the gAccountabilityQuestions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gAccountabilityQuestions_Delete( object sender, RowEventArgs e )
        {
            var accountabilityContext = new AccountabilityContext();
            var questionService = new QuestionService( accountabilityContext );
            var responseService = new ResponseService( accountabilityContext );

            var accountabilityQuestion = questionService.Get( (int)e.RowKeyValue );
            if ( accountabilityQuestion != null )
            {
                string errorMessage;
                if ( !questionService.CanDelete( accountabilityQuestion, out errorMessage ) )
                {
                    maGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }
                List<Response> responseList = responseService.GetResponsesForQuestion( (int)e.RowKeyValue );
                foreach ( Response i in responseList )
                {
                    responseService.Delete( i );
                }
                questionService.Delete( accountabilityQuestion );
                accountabilityContext.SaveChanges();
            }

            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gAccountabilityQuestions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gAccountabilityQuestions_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        protected void mdDialog_SaveClick( object sender, EventArgs e )
        {
            Question question;
            var accountabilityContext = new AccountabilityContext();
            var questionService = new QuestionService( accountabilityContext );

            int questionId = int.Parse( hfQuestionId.Value );

            if ( questionId == 0 )
            {
                question = new Question();
                questionService.Add( question );
            }
            else
            {
                question = questionService.Get( questionId );
            }

            question.ShortForm = tbMdShortForm.Text;
            question.LongForm = tbMdLongForm.Text;
            question.GroupTypeId = int.Parse( hfGroupTypeId.Value );

            if ( !question.IsValid || !Page.IsValid )
            {
                // Controls will render the error messages
                return;
            }

            accountabilityContext.SaveChanges();

            mdDialog.Hide();

            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            var questionService = new QuestionService( new AccountabilityContext() );
            int? groupTypeId = PageParameter( "groupTypeId" ).AsIntegerOrNull();
            GroupType accountabilityGroupType = null;
            if ( groupTypeId.HasValue )
            {
                accountabilityGroupType = _accountabilityGroupType ?? new GroupTypeService( new RockContext() ).Get( groupTypeId.Value );
            }

            if ( accountabilityGroupType == null )
            {
                accountabilityGroupType = new GroupType { Id = 0, Name = "", Description = "" };
            }

            hfGroupTypeId.Value = accountabilityGroupType.Id.ToString();
            var qry = questionService.Queryable( "GroupType" );

            qry = qry.Where( a => a.GroupTypeId == accountabilityGroupType.Id );

            gAccountabilityQuestions.DataSource = qry.OrderBy( a => a.ShortForm ).ToList();

            gAccountabilityQuestions.DataBind();
        }

        /// <summary>
        /// Navigates to detail page.
        /// </summary>
        /// <param name="questionId">The question identifier.</param>
        private void ShowModal( int questionId )
        {
            if ( questionId != 0 )
            {
                var questionService = new QuestionService( new AccountabilityContext() );
                Question question = questionService.Get( questionId );
                mdDialog.Title = "Edit Question";
                hfQuestionId.Value = questionId.ToString();
                tbMdShortForm.Text = question.ShortForm;
                tbMdLongForm.Text = question.LongForm;
                mdDialog.Show();
            }
            else
            {
                mdDialog.Title = "Add Question";
                hfQuestionId.Value = questionId.ToString();
                tbMdShortForm.Text = null;
                tbMdLongForm.Text = null;
                mdDialog.Show();
            }
        }

        /// <summary>
        /// Hides the question block if the group type detail is in edit mode
        /// </summary>
        /// <param name="visible">The bool to determine if the block is hidden or not</param>
        public void SetVisible( bool visible )
        {
            pnlContent.Visible = visible;
        }

        #endregion


    }
}