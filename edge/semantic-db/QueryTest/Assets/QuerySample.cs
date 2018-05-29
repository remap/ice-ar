using UnityEngine;
using System; 
using System.Collections;
using System.Collections.Generic;  

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;  
using MongoDB.Driver.GridFS;  
using MongoDB.Driver.Linq;  

public class QuerySample : MonoBehaviour {

	string connectionString = "mongodb://localhost:27017";

	void Start () {
		AnnotationData[] dataArr = new AnnotationData[5]; //sampledata
		for(int i = 0; i < 5; i++) //populate sample data
		{
			dataArr[i].timestamp = 54386223+i;
			dataArr[i].annotationData = new AnnotationData.ArrayEntry[5];
			for(int j = 0; j < 5; j++){
				dataArr[i].annotationData[j].xleft = 0.98765 + j;
				dataArr[i].annotationData[j].xright = 0.98765 + j;
				dataArr[i].annotationData[j].ytop = 0.98765 + j;
				dataArr[i].annotationData[j].ybottom = 0.98765 + j;
				dataArr[i].annotationData[j].label = "chair";
				var p = j - 1 + 0.98765;
				dataArr[i].annotationData[j].prob = p / j;
			}
		}

		dataArr[3].timestamp = dataArr[2].timestamp;

		var client = new MongoClient(connectionString);
		var server = client.GetServer(); 
		var database = server.GetDatabase("db");
		var entries = database.GetCollection<BsonDocument>("entries");

		//loop for receiving data, in this case just iterating through sampledata
		foreach(var item in dataArr){	
			List<string> temp = new List<string>();
			List<string> seen = new List<string>();
			List<string> objs = new List<string>();

			foreach(var k in item.annotationData){
				temp.Add(k.label);
			}

			foreach(var curr in temp){
				if(seen.Contains(curr) == true){
					continue;
				}

				seen.Add(curr);
				int count = 0;

				for (int i = 0; i < temp.Count; i++)
				{
					string s = temp[i];
					if (s == curr)
					{
						objs.Add(curr + count);
						count++;
					}
				}
			}
			Debug.Log(objs);

			//$in
			BsonArray bArr = new BsonArray(objs);
			var match = new BsonDocument{{"$match",
				new BsonDocument{{"labels", new BsonDocument{{"$in", bArr}}}}}};
			var unwind = new BsonDocument{{"$unwind", "$labels"}};
			var group = new BsonDocument{{"$group", new BsonDocument{{"_id", "$id"}, 
				{"matches", new BsonDocument{{"$sum", 1}}}}}};
			var sort = new BsonDocument{{"$sort", new BsonDocument{{"matches", -1}}}};
			var cursor = new BsonDocument{{"cursor", new BsonDocument{ }}};
			var pipeline = new[] {match, unwind, match, group, sort};
			
			//query = new BsonDocument{{"$in": new BsonArray(new[] {})}}
			//foreach(var elem in objs):
			//	query["$in"].append(elem.encode("utf-8"))
			//	query = {"labels": query}

			//cursor = entr.aggregate(
			//	[{"$match": query},
			//	{"$unwind": "$labels"},
			//	{"$match": query},
			//	{"$group": {
			//		"_id":"$_id",
			//		"matches": {"$sum":1}
			//	}},
			//	{"$sort": {"matches": -1}}]
			//)

			AggregateArgs args = new AggregateArgs();
			args.Pipeline = pipeline;

			var aggregate = entries.Aggregate(args);//.Match(c => c.objs.Any(i => c.labels.Contains(i))).Unwind(c => c.labels).Match(c => c.objs.Any(i => c.labels.Contains(i))).Group(new BsonDocument{{"_id", "$_id"}, {"matches", new BsonDocument("$sum", 1)}}).Sort(new BsonDocument{{"matches", -1}}); //.Limit(20)
			
			//var examples = aggregate.ResultDocuments;

			foreach (var example in aggregate) {  
    			Console.WriteLine(example); 
			}
		}
	}
}

[System.Serializable]
public struct AnnotationData
{
	[System.Serializable]
	public struct ArrayEntry
	{
		public double xleft;
		public double xright;
		public double ytop;
		public double ybottom;
		public string label;
		public double prob;
	}
	public ArrayEntry[] annotationData;
	public int timestamp;
}