using Emgu.CV.CvEnum;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Documents;
using Emgu.CV.Structure;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Diagnostics;
using Npgsql;
using System.Windows.Forms;
using System.Web.UI;

namespace VideoTrack.Controls
{
    public partial class VideoUserControl : System.Windows.Controls.UserControl
    {
        public int GetCameraPKFromDatabase()
        {
            int nCameraPK = 0;

            m_connDatabase.Open();
            NpgsqlCommand cmdUpdateTimestamp = m_connDatabase.CreateCommand();

            // The following sql will insert a marker at the latest
            cmdUpdateTimestamp.CommandText = @"Select ""CameraPK"" from public.""Camera"" where ""URL""=@strRTSPUrl;";
            cmdUpdateTimestamp.Parameters.AddWithValue("strRTSPUrl", this.RTSPUrl);
    
            // We'll use the rtsp string as the way to find the CameraPK
            // This gets loaded either by the json file or by the default
            // load directly from the database.
            NpgsqlDataReader reader = cmdUpdateTimestamp.ExecuteReader();
            if (reader != null)
            {
                if (reader.Read())
                    nCameraPK = reader.GetInt32(reader.GetOrdinal("CameraPK"));
                reader.Close();
            }
            m_connDatabase.Close();
            cmdUpdateTimestamp.Dispose();

            // If the code executed, this will no longer be zero
            return nCameraPK;
        }


        string strLastQueries;

        // Updating a database entry can mean the following:
        //
        // 1.  If a marker is visible and it's last entry is retired
        //     for this camera, or doesn't exist yet for this camera,
        //     add a new entry for it.
        //
        // 2.  If a marker is visible for this camera and has not
        //     moved beyond it's tolerance for movement, then it's
        //     database entry has it's timestamp moved up.
        //
        // 3.  If a marker is visible for this camera and has
        //     moved beyond it's tolerance for movement, then 
        //     the current entry is not retired.  Rather, a new
        //     entry is added so we have a track of movement and
        //     the amount of time, but the current entry is not 
        //     retired.
        public int UpdateDatabaseEntries(List<MarkerPair> listMarkersToUpdate)
        {
            int nNumSucceeded;
            int nNumFailed;

            strLastQueries = "";

            string strSucceeded = "AssetFKs Succeeded = ";
            string strFailed = "AssetFKs Failed = ";

            nNumSucceeded = 0;
            nNumFailed = 0;

            m_connDatabase.Open();
            foreach (MarkerPair pair in listMarkersToUpdate) 
            {
                //pair.PairID;
                // Get the list of database entries 
                // 1.  Marker is visible and is fresh.  Update the row
                //     if it already exists.  Add it if it doesn't already
                //     exist.  We use countdown-1 because it will always
                // en
                if (pair.nCountDown == (MarkerPair.DEFAULT_MARKER_COUNTDOWN-1) && pair.CameraFK>0 )
                {
                    NpgsqlCommand cmdUpdateTimestamp = m_connDatabase.CreateCommand();
                    // The following sql will insert a marker at the latest
                    cmdUpdateTimestamp.CommandText =
                     @"WITH ""Updated"" AS(
	                        Update public.""AssetLocationHistory"" 
	                        SET ""Date""=CURRENT_TIMESTAMP
                            WHERE ""AssetLocationHistoryPK"" =
	                        (   
		                        Select MAX(""AssetLocationHistoryPK"")
		                        FROM public.""AssetLocationHistory""
		                        WHERE ""AssetFK""=@PairID AND ""CameraFK""=@CameraFK
	                        ) AND ""IsStale""=false
	                        Returning ""AssetLocationHistoryPK""
                        ),
                        ""Inserted"" AS (
	                        INSERT INTO public.""AssetLocationHistory"" (""AssetFK"",""CameraFK"",""Date"",""IsStale"")
	                        SELECT @PairID,@CameraFK,CURRENT_TIMESTAMP,false
	                        WHERE NOT EXISTS(SELECT @PairID FROM ""Updated"")
	                        Returning ""AssetLocationHistoryPK""
                        )
                        SELECT ""AssetLocationHistoryPK""
                        FROM ""Updated""
                        UNION ALL
                        SELECT ""AssetLocationHistoryPK""
                        FROM ""Inserted"";";

                    cmdUpdateTimestamp.Parameters.AddWithValue("PairID", pair.PairID);
                    cmdUpdateTimestamp.Parameters.AddWithValue("CameraFK", pair.CameraFK);

                    strLastQueries = cmdUpdateTimestamp.CommandText.Replace("@PairID", pair.PairID.ToString()).Replace("@CameraFK",pair.CameraFK.ToString());

                    // We'll either update or insert a row.  In either case, we'll return
                    // the primary key for use if necessary.
                    NpgsqlDataReader reader = null;
                    try
                    {
                        reader = cmdUpdateTimestamp.ExecuteReader();
                        if (reader != null)
                        {
                            // Read multiple rows if necessary
                            if (reader.Read())
                            {
                                nNumSucceeded++;
                                strSucceeded += pair.CameraFK.ToString() + ":" + pair.PairID.ToString() + ", ";
                            }
                            else
                            {
                                nNumFailed++;
                                strFailed += pair.CameraFK.ToString() + ":" + pair.PairID.ToString() + ", ";
                            }

                        }
                    }
                    catch (PostgresException ex)
                    {
                        // Postgres-specific exception details
                        Debug.WriteLine($"PostgreSQL Error Code: {ex.SqlState}");
                        Debug.WriteLine($"Message: {ex.Message}");
                        Debug.WriteLine($"Detail: {ex.Detail}");
                        Debug.WriteLine($"Hint: {ex.Hint}");
                        Debug.WriteLine($"Position: {ex.Position}");
                        Debug.WriteLine($"Internal Query: {ex.InternalQuery}");
                        Debug.WriteLine($"Where: {ex.Where}");
                        Debug.WriteLine($"ERRRRRRRRRRRRRR");
                        Debug.WriteLine($"Done");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("An exception was caught in UpdateDatabaseEntries: " +  ex.Message);
                        if (ex.InnerException != null)
                        {
                            Debug.WriteLine("    details: " + ex.InnerException.Message);
                        }
                    }
                    finally
                    {
                        if (reader != null)
                            reader.Close();
                    }

                    cmdUpdateTimestamp.Dispose();
                }

                // 2.  Marker may not be visible if the count down is less than
                //   the default initial value.  At the least, it isn't seen clearly
                //   enough to interpret, due to shadows, view angle, partial or complete.
                //   obstruction.  We do not update the timestamp or position.
            }
            m_connDatabase.Close();

            if (nNumSucceeded > 0) 
                Debug.WriteLine(strSucceeded);
            if (nNumFailed > 0)
                Debug.WriteLine(strFailed);

            return nNumSucceeded;
        }


        // Retiring a database entry means marking it as stale.
        public void RetireDatabaseEntries(List<MarkerPair> listMarkersToRetire)
        {
            m_connDatabase.Open();
            foreach (MarkerPair marker in listMarkersToRetire)
            {
                if (marker.CameraFK > 0)
                {
                    NpgsqlCommand cmdRetireAssetMarker = m_connDatabase.CreateCommand();

                    cmdRetireAssetMarker.CommandText =
                     @"WITH ""Updated"" AS(
	                        Update public.""AssetLocationHistory"" 
	                        SET ""IsStale""=true
                            WHERE ""AssetLocationHistoryPK"" =
	                        (   
		                        Select MAX(""AssetLocationHistoryPK"")
		                        FROM public.""AssetLocationHistory""
		                        WHERE ""AssetFK""=@PairID AND ""CameraFK""=@CameraFK
	                        ) 
	                        Returning ""AssetLocationHistoryPK""
                        )
                        SELECT ""AssetLocationHistoryPK""
                        FROM ""Updated"";";

                    cmdRetireAssetMarker.Parameters.AddWithValue("PairID", marker.PairID);
                    cmdRetireAssetMarker.Parameters.AddWithValue("CameraFK", marker.CameraFK);

                    strLastQueries = cmdRetireAssetMarker.CommandText.Replace("@PairID", marker.PairID.ToString()).Replace("@CameraFK", marker.CameraFK.ToString());

                    // We'll either update or insert a row.  In either case, we'll return
                    // the primary key for use if necessary.
                    NpgsqlDataReader reader = null;
                    try
                    {
                        reader = cmdRetireAssetMarker.ExecuteReader();
                        if (reader != null)
                        {
                            // Read multiple rows if necessary
                            if (reader.Read())
                            {
                            }
                        }
                    }
                    catch (PostgresException ex)
                    {
                        // Postgres-specific exception details
                        Debug.WriteLine($"PostgreSQL Error Code: {ex.SqlState}");
                        Debug.WriteLine($"Message: {ex.Message}");
                        Debug.WriteLine($"Detail: {ex.Detail}");
                        Debug.WriteLine($"Hint: {ex.Hint}");
                        Debug.WriteLine($"Position: {ex.Position}");
                        Debug.WriteLine($"Internal Query: {ex.InternalQuery}");
                        Debug.WriteLine($"Where: {ex.Where}");
                        Debug.WriteLine($"ERRRRRRRRRRRRRR");
                        Debug.WriteLine($"Done");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Unknown Exception");
                    }
                    finally
                    {
                        if ( reader != null )
                            reader.Close();
                    }

                    cmdRetireAssetMarker.Dispose();
                }
            }
            m_connDatabase.Close();
        }
    }
}
