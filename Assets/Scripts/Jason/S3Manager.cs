using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using Amazon.S3.Model;
using Amazon.Runtime;
using Amazon.S3.Util;

public class S3Manager : MonoBehaviour
{
    public static S3Manager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if UNITY_ANDROID
    public void UsedOnlyForAOTCodeGeneration()
    {
        //Bug reported on github https://github.com/aws/aws-sdk-net/issues/477
        //IL2CPP restrictions: https://docs.unity3d.com/Manual/ScriptingRestrictions.html
        //Inspired workaround: https://docs.unity3d.com/ScriptReference/AndroidJavaObject.Get.html

        AndroidJavaObject jo = new AndroidJavaObject("android.os.Message");
        int valueString = jo.Get<int>("what");
        string stringValue = jo.Get<string>("what");
    }
#endif

    AmazonS3Client S3Client;
    public int designsToDownload = -1;

    private void Start()
    {
        System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
        UnityInitializer.AttachToGameObject(this.gameObject);
        AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;

        // Initialize the Amazon Cognito credentials provider
        CognitoAWSCredentials credentials = new CognitoAWSCredentials(
            "us-east-2:606e9a0a-5eb6-402d-a840-a53e5c1dd364", // Identity pool ID
            RegionEndpoint.USEast2 // Region
        );

        S3Client = new AmazonS3Client(credentials, RegionEndpoint.USEast2);
    }





    public void ValidateBucketExists(string code)
    {
        bool found = false;

        S3Client.ListBucketsAsync(new ListBucketsRequest(), (responseObject) =>
        {
            if (responseObject.Exception == null)
            {
                responseObject.Response.Buckets.ForEach((s3b) =>
                {
                    //Debug.Log(s3b.BucketName);
                    if (s3b.BucketName == code)
                    {
                        Debug.Log("Found bucket: " + code);
                        ChooserManager.Instance.BucketFound();
                        OnFoundBucket(code);
                        found = true;
                        return;
                    }
                });

                if (!found)
                    BucketNotFound();
            }
        });
    }

    void OnFoundBucket(string bucketName)
    {
        ChooserManager.Instance.SwapPanes();
        CollectDesigns(bucketName);
    }

    void BucketNotFound()
    {
        StartCoroutine(ChooserManager.Instance.BucketNotFound());
    }

    void CollectDesigns(string bucketName)
    {
        var request = new ListObjectsRequest()
        {
            BucketName = bucketName
        };

        S3Client.ListObjectsAsync(request, (responseObject) =>
        {
            if (responseObject.Exception == null)
            {
                designsToDownload = responseObject.Response.S3Objects.Count;

                responseObject.Response.S3Objects.ForEach((o) =>
                {
                     DownloadDesigns(bucketName, o.Key);
                });
            }
            else
            {
                Debug.LogError("AWS Exception");
            }
        });

    }

    void DownloadDesigns(string bucketName, string designName)
    {
        //string saveLocation = @"d:\test";
        string saveLocation = Application.persistentDataPath;

        //Debug.Log("downloading: " + designName + " from " + bucketName);
        S3Client.GetObjectAsync(bucketName, designName, (responseObj) =>
        {
            string data = null;

            var response = responseObj.Response;
            if (response.ResponseStream != null)
            {
                using (var fs = File.Create(saveLocation + Path.DirectorySeparatorChar + designName))
                {
                    byte[] buffer = new byte[81920];
                    int count;
                    while ((count = response.ResponseStream.Read(buffer, 0, buffer.Length)) != 0)
                        fs.Write(buffer, 0, count);
                    fs.Flush();
                    Debug.Log("successfully downloaded: " + designName + " from " + bucketName + " to: " + saveLocation);
                    StartCoroutine(ChooserManager.Instance.DesignReceived(designName, saveLocation));
                }

            }
        });
    }


}




//string response = s3b.Key.Substring(0, s3b.Key.IndexOf('.')-4  Parse(o.Key.Substring(4, o.Key.IndexOf('.') - 4));