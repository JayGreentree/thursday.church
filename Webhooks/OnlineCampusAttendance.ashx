<%@ WebHandler Language="C#" Class="OnlineCampusAttendance" %>

using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Rock;
using Rock.Data;
using Rock.Model;

public class OnlineCampusAttendance : IHttpHandler
{
    public void ProcessRequest( HttpContext context )
    {
        context.Response.ContentType = "text/plain";

        if ( context.Request.HttpMethod != "GET" )
        {
            context.Response.Write( "Invalid request type." );
            return;
        }

        context.Response.StatusCode = 500;
        if ( !string.IsNullOrEmpty( context.Request.QueryString["count"] ) )
        {
            int count;

            if ( int.TryParse( context.Request.QueryString["count"], out count ) )
            {
                context.Response.StatusCode = 200;
                WriteToLog( string.Format( "Received attendance count: {0}", count ) );
                if ( count == 0 )
                {
                    count = 1;
                }

                using ( var rockContext = new RockContext() )
                {
                    try
                    {
                        var service = new MetricValueService( rockContext );
                        DateTime today = DateTime.Now;
                        MetricValue metricValue = null;
                        int metricId = 140;

                        if ( today.DayOfWeek == DayOfWeek.Thursday )
                        {
                            /* Check the 6:45 service */
                            if ( ( today.Hour == 18 && today.Minute >= 45 ) || today.Hour > 17 )
                            {
                                metricId = 6;
                            }
                        }
                        else if ( today.DayOfWeek == DayOfWeek.Sunday )
                        {
                            /* Check the 10:00 service */
                            if ( ( ( today.Hour == 10 && today.Minute >= 45 ) || today.Hour > 10 ) && today.Hour <= 12 )
                            {
                                metricId = 7;
                            }
                            /* Check the 9:00 service */
                            else if ( ( ( today.Hour == 8 && today.Minute >= 45 ) || today.Hour > 8 ) && today.Hour <= 10 )
                            {
                                metricId = 161;
                            }
                        }

                        /* Get the metric value for today of the given metric */
                        metricValue = service.Queryable()
                            .Where( mv => mv.MetricId == metricId && DbFunctions.TruncateTime( mv.MetricValueDateTime ) == DbFunctions.TruncateTime( today.Date ) )
                            .FirstOrDefault();

                        if ( metricValue == null )
                        {
                            metricValue = new MetricValue();
                            service.Add( metricValue );
                            metricValue.MetricId = metricId;
                            metricValue.MetricValueType = MetricValueType.Measure;
                            metricValue.MetricValueDateTime = DateTime.Today;
                            metricValue.XValue = string.Empty;
                            metricValue.YValue = 0;
                            metricValue.Note = string.Empty;
                        }

                        metricValue.YValue += count;
                        rockContext.SaveChanges();

                        context.Response.Write( metricValue.YValue.ToString() );
                    }
                    catch ( Exception ex )
                    {
                        context.Response.Write( ex.ToString() );
                    }
                }
            }
        }
    }

    private void WriteToLog( string message )
    {
        string logFile = HttpContext.Current.Server.MapPath( "~/App_Data/Logs/OnlineCampusAttendanceLog.txt" );

        // Write to the log, but if an ioexception occurs wait a couple seconds and then try again (up to 3 times).
        var maxRetry = 3;
        for ( int retry = 0; retry < maxRetry; retry++ )
        {
            try
            {
                using ( System.IO.FileStream fs = new System.IO.FileStream( logFile, System.IO.FileMode.Append, System.IO.FileAccess.Write ) )
                {
                    using ( System.IO.StreamWriter sw = new System.IO.StreamWriter( fs ) )
                    {
                        sw.WriteLine( string.Format( "{0} - {1}", Rock.RockDateTime.Now.ToString(), message ) );
                        break;
                    }
                }
            }
            catch ( System.IO.IOException )
            {
                if ( retry < maxRetry - 1 )
                {
                    System.Threading.Thread.Sleep( 2000 );
                }
            }
        }

    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}
