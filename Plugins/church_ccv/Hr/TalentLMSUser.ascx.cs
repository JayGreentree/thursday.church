using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Rock;
using Rock.Attribute;

namespace RockWeb.Plugins.church_ccv.Hr
{
    #region Block Attributes

    /// <summary>
    /// TalentLMS dashboard that displays all courses and
    /// course info for current user if their email
    /// address matches an email address in TalentLMS
    /// </summary>
    [DisplayName( "TalntLMS User" )]
    [Category( "CCV > HR" )]
    [Description( "SSO login for Church Online Platform. Add this block to an external facing page and point Church Online Platform at the external page." )]
    [TextField( "TalentLMS API Key", "You can find the API key on the Account & Settings > Basic Settings page in TalentLMS.", true, "", "", 1, "APIKey" )]
    [TextField( "TalentLMS Url", "Your TalentLMS Url", true, "", "", 2, "TalentLMSUrl" )]

    #endregion

    public partial class TalentLMSUser : Rock.Web.UI.RockBlock
    {
        #region Fields

        private string _talentLMSUrl = null;
        private JArray _coursesBlob = null;
        private JObject _userBlob = null;
        private string _userLoginKey = null;

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // Get API key and Url, display error if either imissing
            if ( GetAttributeValue( "APIKey" ).IsNullOrWhiteSpace() || GetAttributeValue( "TalentLMSUrl" ).IsNullOrWhiteSpace() )
            {
                DisplayError( "Block not configured: Please update block settings." );

                // hide the talentLMS dashboard
                pnlTalentLMSDashboard.Visible = false;
                return;
            }

            // Prep for API calls
            string apiKey = GetAttributeValue( "APIKey" );
            _talentLMSUrl = GetAttributeValue( "TalentLMSUrl" );
            string apiUrl = _talentLMSUrl  + "/api/v1/";
            int count = 0;
            string errorMessage = "";
            
            // All Courses
            // Setup API call
            var coursesClient = new RestClient( apiUrl + "courses" );

            coursesClient.Authenticator = new HttpBasicAuthenticator( apiKey, "" );

            var coursesRequest = new RestRequest( Method.GET );

            // Make API call
            coursesClient.ExecuteAsync( coursesRequest, response =>
            {
                // check response status
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Success - Assign response content to coursesBlob field
                    _coursesBlob = JArray.Parse( response.Content );
                }
                else
                {
                    // Failed - pass error message
                    errorMessage = response.Content;
                }

                // API complete, increment count
                Interlocked.Increment( ref count );
            });
            
            // User Courses
            // Setup API call
            var userClient = new RestClient( apiUrl + "users/email:" + CurrentUser.Person.Email );

            userClient.Authenticator = new HttpBasicAuthenticator( apiKey, "" );

            var usersRequest = new RestRequest( Method.GET );

            // Make API call
            userClient.ExecuteAsync( usersRequest, response =>
            {
                // check response status
                if ( response.StatusCode == System.Net.HttpStatusCode.OK )
                {
                    // Success
                    // Assign response content to userBlob field
                    _userBlob = JObject.Parse( response.Content );

                    // parse out user key
                    var userLoginKeyArray = _userBlob["login_key"].ToString().Split( ':' );

                    if (userLoginKeyArray.Length > 1)
                    {
                        // parse success - Assign value to userLoginKey field
                        _userLoginKey = userLoginKeyArray[2];
                    }
                    
                    if ( _userLoginKey.IsNullOrWhiteSpace() )
                    {
                        // parse failed - set error message
                        errorMessage = "Failed to poplate user key";
                    }
                }
                else
                {
                    // Failed - pass error message
                    errorMessage = response.Content;
                }

                // API complete, increment counter
                Interlocked.Increment( ref count );
            } );

            // Sleep thread until both API's complete
            while ( count != 2 )
            {
                Thread.Sleep( 1 );
            }

            // Check that blob content has been set before proceeding
            if ( _coursesBlob == null || _userBlob == null )
            {
                // Missing blob content
                // check if error was user does not exist and hide talentLMS panel
                if ( errorMessage.IsNotNullOrWhitespace() && errorMessage.Contains("The requested user does not exist") ) {
                    pnlTalentLMS.Visible = false;
                    return;
                } else
                {
                    // Something else failed and no error was returned - set default message.
                    errorMessage = "Failed to load TalentLMS.  Please contact your administrator.";
                }

                DisplayError( errorMessage );

                // hide talentLMS dashboard
                pnlTalentLMSDashboard.Visible = false;
                return;
            } 

            // Render user panel
            RenderUserPanel();

            // Render courses panel
            RenderCoursesPanel();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Render the user courses talentlms panel
        /// </summary>
        private void RenderUserPanel()
        {
            // Build enrolled courses list with name, progress status and launch button
            var courses = from c in _userBlob["courses"].Children()
                          select new
                          {
                              Name = CreateCourseName( ( string ) c["name"], (string) ( c["expired_on"] ) ),
                              Progress = CreateCourseStatusBadge( ( string ) c["completion_status"], Int32.Parse( ( string ) c["completion_percentage"] ) ),
                              Action = CreateCourseLaunchButton( ( string ) c["id"] )
                          };

            // bind list to user grid
            gUserGrid.DataSource = courses.ToList();
            gUserGrid.DataBind();
        }

        /// <summary>
        /// Render the all courses talentlms panel
        /// </summary>
        private void RenderCoursesPanel()
        {
            // Build courses list with name and more info button
            var courses = from c in _coursesBlob.Children()
                            select new
                            {
                                Name = ( string ) c["name"],
                                Action = CreateCourseInfoButton( ( string ) c["id"])
                            };

            // bind list to courses grid
            gCourseGrid.DataSource = courses.ToList();
            gCourseGrid.DataBind();
        }

        /// <summary>
        /// Create an html course name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="expires"></param>
        /// <returns></returns>
        private string CreateCourseName( string name, string expiration)
        {
            if ( expiration.IsNullOrWhiteSpace() )
            {
                // Course has no expiration date - return just the name
                return String.Format( "<div class=\"course-name\">{0}</div>", name );
            } else
            {
                // Course has expiration date - Convert date string into DateTime object
                DateTime expirationDate = DateTime.ParseExact( expiration, "dd/MM/yyyy, HH:mm:ss", CultureInfo.InvariantCulture );

                // Set default expiration text
                string expirationText = "expires";

                // Check if expiration date is in past
                if(expirationDate < DateTime.Now)
                {
                    // In Past - change expiration text
                    expirationText = "expired";
                }

                // Return Course Name
                return String.Format( "<div class=\"course-name\">{0}<span class=\"{1}\">{1} on {2}</span></div>", name, expirationText, expirationDate.ToString("MM/dd/yyyy") );
            }
        }

        /// <summary>
        /// Create an html course status badge
        /// </summary>
        /// <param name="status"></param>
        /// <param name="percentage"></param>
        /// <returns></returns>
        private string CreateCourseStatusBadge( string status, int percentage )
        {
            if (status == "completed")
            {
                // Course complete
                return "<span class=\"progress-completed\">Completed</span>";
            } else
            {
                // Course in progress
                return String.Format( "<div class=\"progress\"><div class=\"progress-bar\" role=\"progressbar\" aria-valuenow=\"{0}\" aria-valuemin=\"0\" aria-valuemax=\"100\" style=\"width:{0}%\">{0}%</div></div>", percentage );
            }        
        }

        /// <summary>
        /// Create an html button to launch a course
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        private string CreateCourseLaunchButton( string courseId )
        {
            return String.Format( "<a href=\"{0}/index/gotocourse/key:{1},course_id:{2}\" target=\"_blank\" class=\"btn btn-primary btn-talentlms\">Launch</a></br>", _talentLMSUrl, _userLoginKey, courseId );
        }

        /// <summary>
        /// Create an html button to go to course info
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        private string CreateCourseInfoButton( string courseId )
        {
            return String.Format( "<a href=\"{0}/catalog/info/id:{1}\" target=\"_blank\" class=\"btn btn-primary btn-talentlms\">More Info</a></br>", _talentLMSUrl, courseId );
        }

        /// <summary>
        /// Display an error message
        /// </summary>
        /// <param name="message"></param>
        private void DisplayError( string message )
        {
            pnlMessage.Controls.Clear();
            pnlMessage.Controls.Add( new LiteralControl( message ) );
            pnlMessage.Visible = true;
        }

        #endregion
    }
}
