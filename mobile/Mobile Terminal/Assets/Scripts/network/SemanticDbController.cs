/**
 * Copyright (C) 2017 Regents of the University of California.
 * @author: Peter Gusev <peter@remap.ucla.edu>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * A copy of the GNU Lesser General Public License is in the file COPYING.
 */
#define ENABLE_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public delegate void OnDbResult(DbReply reply, string errorMsg);

[Serializable]
public class DbReplyEntry {
    public string frameName;
    public float simLevel;
}

[Serializable]
public class DbReply {
    public DbReplyEntry[] entries;
}

public class SemanticDbController : ILogComponent  {
    private string semanticDbRequestUrl_;
    private Dictionary<string, OnDbResult> callbacks_;

    public SemanticDbController(string url) 
    {
        semanticDbRequestUrl_ = url;
        callbacks_ = new Dictionary<string, OnDbResult>();
    }

    ~SemanticDbController()
    {

    }

    public void runQuery(string jsonAnnotationString, OnDbResult onDbResult)
    {
        // NOTE: it is expected that jsonAnnotationString is a json array, i.e. it looks like
        // [ { annotation1 }, { annotation2 }, ... ]

        string compactString = jsonAnnotationString.Replace(System.Environment.NewLine, "");
        string queryString = "{\"annotations\":"+compactString+"}";

        callbacks_[queryString] = onDbResult;
        UnityMainThreadDispatcher.Instance().Enqueue(runDbQuery(queryString));
    }

    IEnumerator runDbQuery(string queryString)
    {
        var data = System.Text.Encoding.ASCII.GetBytes(queryString);

        using (UnityWebRequest www = new UnityWebRequest(semanticDbRequestUrl_))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            www.uploadHandler = new UploadHandlerRaw( data );
            //General purpose DownloadHandler subclass. Must be explicitly instantiated if not calling 
            //UnityWebRequest.post() or .get()
            www.downloadHandler = new DownloadHandlerBuffer(); 
            
            yield return www.SendWebRequest();

            try {
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.ErrorFormat(this, "query error {0}", www.error);
                    callbacks_[queryString](null, www.error);
                }
                else
                {
                    Debug.LogFormat("query result {0}"+www.downloadHandler.text);
                    var reply = JsonUtility.FromJson<DbReply>(www.downloadHandler.text);

                    callbacks_[queryString](reply, "");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(this, e);
                callbacks_[queryString](null, e.Message);
            }

            if (queryString != null)
                callbacks_.Remove(queryString);
        }
    }

    public string getLogComponentName()
    {
        return "semantic-db";
    }

    public bool isLoggingEnabled()
    {
#if ENABLE_LOG
        return true;
#else
        return false;
#endif
    }
}
