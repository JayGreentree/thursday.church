// <copyright>
// Copyright by the Spark Development Network
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.church_ccv.Security
{
    [DisplayName( "CCV Captive Portal Form" )]
    [Category( "CCV > Security" )]
    [Description( "Form used to access to Wi-Fi." )]

    #region Block Settings
    [TextField(
        name: "MAC Address Paramameter",
        description: "The query string parameter used for the MAC Address",
        defaultValue: "client_mac",
        order: 0,
        key: "MacAddressParam" )]
    [TextField(
        name: "Release Link",
        description: "The full URL to redirect users to after registration.",
        order: 1,
        key: "ReleaseLink" )]
    [BooleanField(
        name: "Show Name",
        description: "Show or hide the Name fields. If it is visible then it will be required.",
        defaultValue: true,
        order: 2,
        key: "ShowName",
        IsRequired = true )]
    [BooleanField(
        name: "Show Mobile Phone",
        description: "Show or hide the Mobile Phone Number field. If it is visible then it will be required.",
        defaultValue: true,
        order: 3,
        key: "ShowMobilePhone",
        IsRequired = true )]
    [BooleanField(
        name: "Show Email",
        description: "Show or hide the Email field. If it is visible then it will be required.",
        defaultValue: true,
        order: 4,
        key: "ShowEmail",
        IsRequired = true )]
    [TextField(
        name: "Title Text",
        description: "Title Text to display",
        defaultValue: "Thank you for joining us.",
        order: 5,
        key: "TitleText" )]
    [TextField(
        name: "Button Text",
        description: "Text to display on the button",
        defaultValue: "Connect",
        order: 7,
        key: "ButtonText" )]
    [DefinedValueField(
        definedTypeGuid: Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON,
        name: "New Person Record Type",
        description: "The person type to assign to new persons created by Captive Portal.",
        defaultValue: Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON,
        order: 9,
        key: "NewPersonRecordType" )]
    [DefinedValueField(
        definedTypeGuid: Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS,
        name: "New Person Record Status",
        description: "The record status to assign to new persons created by Captive Portal.",
        defaultValue: Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE,
        order: 10,
        key: "NewPersonRecordStatus" )]
    [DefinedValueField(
        definedTypeGuid: Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS,
        name: "New Person Connection Status",
        description: "The connection status to assign to new persons created by Captive Portal",
        defaultValue: Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_VISITOR,
        order: 11,
        key: "NewPersonConnectionStatus" )]

    #endregion Block Settings
    public partial class CCVCaptivePortalForm : RockBlock
    {
        /// <summary>
        /// The user agents to ignore. UA strings that begin with one of these will be ignored.
        /// This is to fix Apple devices loading the page with its CaptiveNetwork WISPr UA and messing
        /// up the device info, which is parsed from the UA. Ignoring "CaptiveNetworkSupport*"
        /// will fix 100% of current known issues, if more than a few come up we should put this
        /// into the DB as DefinedType/DefinedValues.
        /// </summary>
        private List<string> _userAgentsToIgnore = new List<string>()
        {
            "CaptiveNetworkSupport"
        };

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            nbAlert.Visible = false;

            // Go through the UA ignore list and don't load anything we don't care about or want.
            foreach ( string userAgentToIgnore in _userAgentsToIgnore )
            {
                if ( Request.UserAgent.StartsWith( userAgentToIgnore ) )
                {
                    return;
                }
            }

            if ( !IsPostBack )
            {
                string macAddress = RockPage.PageParameter( GetAttributeValue( "MacAddressParam" ) );
                if ( string.IsNullOrWhiteSpace( macAddress ) || !macAddress.IsValidMacAddress() )
                {
                    nbAlert.Text = "Missing or invalid MAC Address";
                    nbAlert.Visible = true;
                    ShowControls( false );
                    return;
                }

                string releaseLink = GetAttributeValue( "ReleaseLink" );
                if ( string.IsNullOrWhiteSpace( releaseLink ) || !releaseLink.IsValidUrl() )
                {
                    nbAlert.Text = "Missing or invalid Release Link";
                    nbAlert.Visible = true;
                    ShowControls( false );
                    return;
                }

                // Save the supplied MAC address to the page removing any non-Alphanumeric characters
                macAddress = macAddress.RemoveAllNonAlphaNumericCharacters();
                hfMacAddress.Value = macAddress;

                // create or get device
                PersonalDeviceService personalDeviceService = new PersonalDeviceService( new RockContext() );
                PersonalDevice personalDevice = null;

                if ( DoesPersonalDeviceExist( macAddress ) )
                {
                    personalDevice = VerifyDeviceInfo( macAddress );
                }
                else
                {
                    personalDevice = CreateDevice( macAddress );
                }

                // We are going to create this everytime they hit the captive portal page. Otherwise if the device is saved but not linked to an actual user (not the fake one created here),
                // and then deleted by the user/browser/software, then they'll never get the cookie again and won't automatically get linked by RockPage.
                CreateDeviceCookie( macAddress );

                // See if user is logged in and link the alias to the device.
                if ( CurrentPerson != null )
                {
                    Prefill( CurrentPerson );
                    RockPage.LinkPersonAliasToDevice( ( int ) CurrentPersonAliasId, macAddress );
                }

                // Set the title text
                string titleText = GetAttributeValue( "TitleText" );
                if ( titleText.IsNotNullOrWhiteSpace() )
                {
                    lblTitleText.Text = titleText;
                }

                // Direct connect if no controls are visible
                if ( !ShowControls() )
                {
                    // Nothing to show means nothing to enter. Redirect user back to FP with the primary alias ID and query string
                    if ( IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) )
                    {
                        nbAlert.Text = string.Format( "If you did not have Administrative permissions on this block you would have been redirected to: <a href='{0}'>{0}</a>.", CreateRedirectUrl( null ) );
                    }
                    else
                    {
                        Response.Redirect( CreateRedirectUrl( null ) );
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Doeses a personal device exist for the provided MAC address
        /// </summary>
        /// <param name="macAddress">The mac address.</param>
        /// <returns></returns>
        private bool DoesPersonalDeviceExist( string macAddress )
        {
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( new RockContext() );
            return personalDeviceService.GetByMACAddress( macAddress ) == null ? false : true;
        }

        /// <summary>
        /// Creates the device if new.
        /// </summary>
        /// <returns>Returns true if the device was created, false it already existed</returns>
        private PersonalDevice CreateDevice( string macAddress )
        {
            UAParser.ClientInfo client = UAParser.Parser.GetDefault().Parse( Request.UserAgent );

            RockContext rockContext = new RockContext();
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( rockContext );

            PersonalDevice personalDevice = new PersonalDevice();
            personalDevice.MACAddress = macAddress;

            personalDevice.PersonalDeviceTypeValueId = GetDeviceTypeValueId();
            personalDevice.PlatformValueId = GetDevicePlatformValueId( client );
            personalDevice.DeviceVersion = GetDeviceOsVersion( client );

            personalDeviceService.Add( personalDevice );
            rockContext.SaveChanges();

            return personalDevice;
        }

        /// <summary>
        /// Gets the current device platform info and updates the obj if needed.
        /// </summary>
        /// <param name="personalDevice">The personal device.</param>
        private PersonalDevice VerifyDeviceInfo( string macAddress )
        {
            UAParser.ClientInfo client = UAParser.Parser.GetDefault().Parse( Request.UserAgent );

            RockContext rockContext = new RockContext();
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( rockContext );

            PersonalDevice personalDevice = personalDeviceService.GetByMACAddress( macAddress );
            personalDevice.PersonalDeviceTypeValueId = GetDeviceTypeValueId();
            personalDevice.PlatformValueId = GetDevicePlatformValueId( client );
            personalDevice.DeviceVersion = GetDeviceOsVersion( client );

            rockContext.SaveChanges();

            return personalDevice;
        }

        /// <summary>
        /// Uses the Request information to determine if the device is mobile or not
        /// </summary>
        /// <returns>DevinedValueId for "Mobile" or "Computer", Mobile includes Tablet. Null if there is a data issue and the DefinedType is missing</returns>
        private int? GetDeviceTypeValueId()
        {
            // Get the device type Mobile or Computer
            DefinedTypeCache definedTypeCache = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSONAL_DEVICE_TYPE.AsGuid() );
            DefinedValueCache definedValueCache = null;

            var clientType = InteractionDeviceType.GetClientType( Request.UserAgent );
            clientType = clientType == "Mobile" || clientType == "Tablet" ? "Mobile" : "Computer";

            if ( definedTypeCache != null )
            {
                definedValueCache = definedTypeCache.DefinedValues.FirstOrDefault( v => v.Value == clientType );

                if ( definedValueCache == null )
                {
                    definedValueCache = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSONAL_DEVICE_TYPE_COMPUTER.AsGuid() );
                }

                return definedValueCache.Id;
            }

            return null;
        }

        /// <summary>
        /// Parses ClientInfo to find the OS family
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns>DefinedValueId for the found OS. Uses "Other" if the OS is not in DefinedValue. Null if there is a data issue and the DefinedType is missing</returns>
        private int? GetDevicePlatformValueId( UAParser.ClientInfo client )
        {
            // get the OS
            string platform = client.OS.Family.Split( ' ' ).First();

            DefinedTypeCache definedTypeCache = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSONAL_DEVICE_PLATFORM.AsGuid() );
            DefinedValueCache definedValueCache = null;
            if ( definedTypeCache != null )
            {
                definedValueCache = definedTypeCache.DefinedValues.FirstOrDefault( v => v.Value == platform );

                if ( definedValueCache == null )
                {
                    definedValueCache = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSONAL_DEVICE_PLATFORM_OTHER.AsGuid() );
                }

                return definedValueCache.Id;
            }

            return null;
        }

        /// <summary>
        /// Parses ClientInfo and gets the device os version. If it cannot be determined returns the OS family string without the platform
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns></returns>
        private string GetDeviceOsVersion( UAParser.ClientInfo client )
        {
            if ( client.OS.Major == null )
            {
                string platform = client.OS.Family.Split( ' ' ).First();
                return client.OS.Family.Replace( platform, string.Empty ).Trim();
            }

            return string.Format(
                "{0}.{1}.{2}.{3}",
                client.OS.Major ?? "0",
                client.OS.Minor ?? "0",
                client.OS.Patch ?? "0",
                client.OS.PatchMinor ?? "0" );
        }

        /// <summary>
        /// Creates the device cookie if it does not exist.
        /// </summary>
        private void CreateDeviceCookie( string macAddress )
        {
            if ( Request.Cookies["rock_wifi"] == null )
            {
                HttpCookie httpcookie = new HttpCookie( "rock_wifi" );
                httpcookie.Expires = DateTime.MaxValue;
                httpcookie.Values.Add( "ROCK_PERSONALDEVICE_ADDRESS", macAddress );
                Response.Cookies.Add( httpcookie );
            }
        }

        /// <summary>
        /// Prefills the visible fields with info from the specified rock person
        /// if there is a logged in user than the name fields will be disabled.
        /// </summary>
        /// <param name="rockUserId">The rock user identifier.</param>
        protected void Prefill( Person person )
        {
            if ( person == null )
            {
                return;
            }

            if ( tbFirstName.Visible == true )
            {
                tbFirstName.Text = person.FirstName;
                tbFirstName.Enabled = CurrentPerson == null;

                tbLastName.Text = person.LastName;
                tbLastName.Enabled = CurrentPerson == null;
            }

            if ( tbMobilePhone.Visible )
            {
                PhoneNumberService phoneNumberService = new PhoneNumberService( new RockContext() );
                PhoneNumber phoneNumber = phoneNumberService.GetNumberByPersonIdAndType( person.Id, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );
                tbMobilePhone.Text = phoneNumber == null ? string.Empty : phoneNumber.Number;
            }

            if ( tbEmail.Visible == true )
            {
                tbEmail.Text = person.Email;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnConnect control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConnect_Click( object sender, EventArgs e )
        {
            // We know there is a device with the stored MAC
            // If we have an alias ID then we have all data needed and can redirect the user to frontporch
            // also if hfPersonAliasId has a value at this time it has already been linked to the device.
            if ( CurrentPerson != null )
            {
                UpdatePersonInfo();
                Response.Redirect( CreateRedirectUrl( CurrentPersonAliasId ) );
                return;
            }

            int? primaryAliasId = LinkDeviceToPerson();
            if ( primaryAliasId != null )
            {
                Response.Redirect( CreateRedirectUrl( primaryAliasId ) );
                return;
            }

            // Send them back to Front Porch without user info
            Response.Redirect( CreateRedirectUrl( null ) );
        }

        /// <summary>
        /// Try to link the device to a person and return the primary alias ID if successful
        /// </summary>
        /// <returns>true if device successfully linked to a person</returns>
        protected int? LinkDeviceToPerson()
        {
            // At this point the user is not logged in and not found by looking up the device
            // So lets try to find the user using entered info and then link them to the device.
            PersonService personService = new PersonService( new RockContext() );
            int mobilePhoneTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;
            Person person = null;
            string mobilePhoneNumber = string.Empty;

            // Looking for a 100% match
            if ( tbFirstName.Visible && tbLastName.Visible && tbEmail.Visible && tbMobilePhone.Visible )
            {
                mobilePhoneNumber = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters();

                var personQuery = new PersonService.PersonMatchQuery( tbFirstName.Text, tbLastName.Text, tbEmail.Text, mobilePhoneNumber );
                person = personService.FindPerson( personQuery, true );

                if ( person.IsNotNull() )
                {
                    RockPage.LinkPersonAliasToDevice( person.PrimaryAlias.Id, hfMacAddress.Value );
                    return person.PrimaryAliasId;
                }
                else
                {
                    // If no known person record then create one since we have the minimum info required
                    person = CreateAndSaveNewPerson();

                    // Link new device to person alias
                    RockPage.LinkPersonAliasToDevice( person.PrimaryAlias.Id, hfMacAddress.Value );
                    return person.PrimaryAlias.Id;
                }
            }

            // Look for minimum info
            if ( tbFirstName.Visible && tbLastName.Visible && ( tbMobilePhone.Visible || tbEmail.Visible ) )
            {
                // If no known person record then create one since we have the minimum info required
                person = CreateAndSaveNewPerson();

                // Link new device to person alias
                RockPage.LinkPersonAliasToDevice( person.PrimaryAlias.Id, hfMacAddress.Value );
                return person.PrimaryAlias.Id;
            }

            // Just match off phone number if no other fields are showing.
            if ( tbMobilePhone.Visible && !tbFirstName.Visible && !tbLastName.Visible && !tbEmail.Visible )
            {
                mobilePhoneNumber = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters();
                person = personService.Queryable().Where( p => p.PhoneNumbers.Where( n => n.NumberTypeValueId == mobilePhoneTypeId ).FirstOrDefault().Number == mobilePhoneNumber ).FirstOrDefault();
                if ( person != null )
                {
                    RockPage.LinkPersonAliasToDevice( person.PrimaryAlias.Id, hfMacAddress.Value );
                    return person.PrimaryAliasId;
                }
            }

            // Unable to find an existing user and we don't have the minimium info to create one.
            // We'll let Rock.Page and the cookie created in OnLoad() link the device to a user when they are logged in.
            // This will not work if this page is loaded into a Captive Network Assistant page as the cookie will not persist.
            return null;
        }

        protected Person CreateAndSaveNewPerson()
        {
            int mobilePhoneTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;

            var recordTypeValue = DefinedValueCache.Read( GetAttributeValue( "NewPersonRecordType" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() );
            var recordStatusValue = DefinedValueCache.Read( GetAttributeValue( "NewPersonRecordStatus" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() );
            var connectionStatusValue = DefinedValueCache.Read( GetAttributeValue( "NewPersonConnectionStatus" ).AsGuid() ) ?? DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_VISITOR.AsGuid() );

            var person = new Person
            {
                FirstName = tbFirstName.Text,
                LastName = tbLastName.Text,
                Email = tbEmail.Text,
                RecordTypeValueId = recordTypeValue != null ? recordTypeValue.Id : ( int? ) null,
                RecordStatusValueId = recordStatusValue != null ? recordStatusValue.Id : ( int? ) null,
                ConnectionStatusValueId = connectionStatusValue != null ? connectionStatusValue.Id : ( int? ) null
            };

            if ( tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters().IsNotNullOrWhiteSpace() )
            {
                person.PhoneNumbers = new List<PhoneNumber>() { new PhoneNumber { IsSystem = false, Number = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters(), NumberTypeValueId = mobilePhoneTypeId } };
            }

            PersonService.SaveNewPerson( person, new RockContext() );
            return person;
        }

        /// <summary>
        /// Redirects the specified person to the release link URL
        /// </summary>
        /// <param name="primaryAliasId">if null then the id parameter is not created for the returned URL</param>
        /// <returns></returns>
        protected string CreateRedirectUrl( int? primaryAliasId )
        {
            string s = string.Empty;

            if ( primaryAliasId != null )
            {
                s = string.Format( "{0}?id={1}&{2}", GetAttributeValue( "ReleaseLink" ), primaryAliasId, Request.QueryString );
                return s;
            }

            s = string.Format( "{0}?{1}", GetAttributeValue( "ReleaseLink" ), Request.QueryString );
            return s;
        }

        /// <summary>
        /// Shows the controls according to the attribute values. If they are visible then they are also required.
        /// </summary>
        /// <returns>If any control is visible then true, else false.</returns>
        protected bool ShowControls( bool isEnabled = true )
        {
            tbFirstName.Visible = GetAttributeValue( "ShowName" ).AsBoolean();
            tbFirstName.Required = GetAttributeValue( "ShowName" ).AsBoolean();
            tbFirstName.Enabled = isEnabled;

            tbLastName.Visible = GetAttributeValue( "ShowName" ).AsBoolean();
            tbLastName.Required = GetAttributeValue( "ShowName" ).AsBoolean();
            tbLastName.Enabled = isEnabled;

            tbMobilePhone.Visible = GetAttributeValue( "ShowMobilePhone" ).AsBoolean();
            tbMobilePhone.Required = GetAttributeValue( "ShowMobilePhone" ).AsBoolean();
            tbMobilePhone.Enabled = isEnabled;

            tbEmail.Visible = GetAttributeValue( "ShowEmail" ).AsBoolean();
            tbEmail.Required = GetAttributeValue( "ShowEmail" ).AsBoolean();
            tbEmail.Enabled = isEnabled;

            btnConnect.Text = isEnabled ? GetAttributeValue( "ButtonText" ) : "Connect";
            btnConnect.Enabled = isEnabled;

            if ( tbFirstName.Visible || tbLastName.Visible || tbMobilePhone.Visible || tbEmail.Visible )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the CurrentPerson email and mobile phone number with the entered values if they are different.
        /// Does nothing if there is no CurrentPerson
        /// </summary>
        private void UpdatePersonInfo()
        {
            if ( CurrentPerson == null )
            {
                return;
            }

            using ( RockContext rockContext = new RockContext() )
            {
                Person person = new PersonService( rockContext ).Get( ( int ) CurrentPersonId );
                person.Email = tbEmail.Text;

                int mobilePhoneTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;
                if ( !person.PhoneNumbers.Where( n => n.NumberTypeValueId == mobilePhoneTypeId ).Any() &&
                    tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters().IsNotNullOrWhiteSpace() )
                {
                    person.PhoneNumbers.Add( new PhoneNumber { IsSystem = false, Number = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters(), NumberTypeValueId = mobilePhoneTypeId } );
                }
                else
                {
                    PhoneNumber phoneNumber = person.PhoneNumbers.Where( p => p.NumberTypeValueId == mobilePhoneTypeId ).FirstOrDefault();
                    if ( phoneNumber != null )
                    {
                        phoneNumber.Number = tbMobilePhone.Text.RemoveAllNonAlphaNumericCharacters();
                    }
                }

                rockContext.SaveChanges();
            }
        }
    }
}