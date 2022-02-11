/*
 * Copyright (c) 2019 Dummiesman
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
*/

using Dummiesman;
using System.Collections.Generic;
using UnityEngine;
using MapMod.objLoader;
using MapMod;

namespace Dummiesman {
public class OBJObjectBuilder {
	//
	public int PushedFaceCount { get; private set; } = 0;

	//stuff passed in by ctor
	private OBJLoader _loader;
	private string _name;

	private Dictionary<ObjLoopHash, int> _globalIndexRemap = new Dictionary<ObjLoopHash, int>();
	private Dictionary<string, List<int>> _materialIndices = new Dictionary<string, List<int>>();
	private List<int> _currentIndexList;
	private string _lastMaterial = null;

	//our local vert/normal/uv
	private List<Vector3> _vertices = new List<Vector3>();
	private List<Vector3> _normals = new List<Vector3>();
	private List<Vector2> _uvs = new List<Vector2>();

	//this will be set if the model has no normals or missing normal info
	private bool recalculateNormals = false;

	/// <summary>
	/// Loop hasher helper class
	/// </summary>
	private class ObjLoopHash {
		public int vertexIndex;
		public int normalIndex;
		public int uvIndex;

		public override bool Equals(object obj) {
			if (!(obj is ObjLoopHash))
				return false;

			var hash = obj as ObjLoopHash;
			return (hash.vertexIndex == vertexIndex) && (hash.uvIndex == uvIndex) && (hash.normalIndex == normalIndex);
		}

		public override int GetHashCode() {
			int hc = 3;
			hc = unchecked(hc * 314159 + vertexIndex);
			hc = unchecked(hc * 314159 + normalIndex);
			hc = unchecked(hc * 314159 + uvIndex);
			return hc;
		}
	}

	public GameObject Build() {
		var go = new GameObject(_name);

		//add meshrenderer
		var mr = go.AddComponent<MeshRenderer>();
		int submesh = 0;


		//locate the material for each submesh
		Material[] materialArray = new Material[_materialIndices.Count];
		foreach (var kvp in _materialIndices) {
			Material material = null;
			if (_loader.Materials == null) {
				material = OBJLoaderHelper.CreateNullMaterial();
				material.name = kvp.Key;
			} else {
				if (!_loader.Materials.TryGetValue(kvp.Key, out material)) {
					material = OBJLoaderHelper.CreateNullMaterial();
					material.name = kvp.Key;
					_loader.Materials[kvp.Key] = material;
				}
			}
			materialArray[submesh] = material;
			submesh++;
		}
		mr.sharedMaterials = materialArray;

		//add meshfilter
		var mf = go.AddComponent<MeshFilter>();
		submesh = 0;

		var msh = new Mesh() {
			name = _name, 
			indexFormat = (_vertices.Count > 65535) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16,
			subMeshCount = _materialIndices.Count 
		};

		//set vertex data
		msh.SetVertices(Conversion.ToIl2CppList<Vector3>(_vertices));
		msh.SetNormals(Conversion.ToIl2CppList<Vector3>(_normals));
		// msh.SetUVs(0, Conversion.ToIl2CppList<Vector2>(_uvs));
        if (go.name.Contains("textured") || Plugin.allObjectsTextured) {
            Vector2[] uvs = new Vector2[_uvs.Count];
            for(int i = 0; i < _uvs.Count; i++) {
                uvs[i] = _uvs[i];
            }
            msh.uv = uvs;
        }

        //set faces
        foreach (var kvp in _materialIndices) {
		msh.SetTriangles(Conversion.ToIl2CppList<int>(kvp.Value), submesh);
		submesh++;
		}

		//recalculations
		if (recalculateNormals)
			msh.RecalculateNormals();
		// msh.RecalculateTangents();
		msh.RecalculateBounds();

		mf.sharedMesh = msh;

        if (go.name.Contains("spawnzone"))
        {
            MonoBehaviourPublicVesiUnique zone = GameObject.Find("/SpawnZoneManager").transform.GetChild(0).gameObject.GetComponent<MonoBehaviourPublicVesiUnique>();
            Vector3 pos = mr.bounds.center;
            pos.x *= -1;
            zone.gameObject.transform.position = pos;
            zone.size = mr.bounds.size;
            GameObject.Destroy(go);
            return null;
        }
            //mr.material.color = new Color(0.3f,0.3f,0.3f,1);
            if (!go.name.Contains("nocol"))
            {
                if (go.name.Contains("ice1"))
                    go.tag = "IceButNotThatIcy";
                if (go.name.Contains("ice2"))
                    go.tag = "Ice";
                MeshCollider mcol = go.AddComponent<MeshCollider>();
                mcol.sharedMesh = msh;
                go.layer = 6;
                if (go.name.Contains("ladder"))
                {
                    if (go.name.Contains("flip"))
                    {
                        go.transform.RotateAround(mcol.bounds.center, Vector3.up, 180);
                    }
                    if (go.name.Contains("rot90"))
                    {
                        go.transform.RotateAround(mcol.bounds.center, Vector3.up, 90);
                    }
                    go.layer = 14;
                    go.AddComponent<MonoBehaviourPublicLi1CoonUnique>();
                    //mr.material.color = new Color(1f, 0.5f, 0.0f, 1);
                    mcol.convex = true;
                    mcol.isTrigger = true;
                }
                if (go.name.Contains("tire"))
                {
                    go.layer = 14;
                    mcol.convex = true;
                    mcol.isTrigger = true;
                    MonoBehaviourPublicSiBopuSiUnique tireScript = go.AddComponent<MonoBehaviourPublicSiBopuSiUnique>();
                    tireScript.field_Private_Boolean_0 = true;
                    tireScript.field_Private_Single_0 = 0.25f;
                    tireScript.pushForce = 35;
                    string forceValue = Plugin.tryGetValue(go.name, "tforce");
                    if (forceValue != null)
                        tireScript.pushForce = int.Parse(forceValue);
                    //mr.material.color = new Color(0f,0f,0f,1f);
                }
                if (go.name.Contains("boom")) {
                    MonoBehaviourPublicSicofoSimuupInSiboVeUnique script = go.AddComponent<MonoBehaviourPublicSicofoSimuupInSiboVeUnique>();
                    script.force = 40;
                    script.upForce = 15;
                    script.field_Private_Boolean_0 = true;
                    script.cooldown = 0.5f;
                    string forceValue = Plugin.tryGetValue(go.name,"bforce");
                    if (forceValue != null)
                        script.force = int.Parse(forceValue);
                    string upForceValue = Plugin.tryGetValue(go.name, "upforce");
                    if (upForceValue != null)
                        script.upForce = int.Parse(upForceValue);
                }
                if (go.name.Contains("spinner")) {
                    Spinner spinner = go.AddComponent<Spinner>();
                    string speedValue = Plugin.tryGetValue(go.name, "rspeed");
                    if (speedValue != null)
                        spinner.speed = int.Parse(speedValue);
                }
                if (go.name.Contains("checkpoint"))
                    go.AddComponent<Checkpoint>();
            }
            string rotValue = Plugin.tryGetValue(go.name,"rot");
            if (rotValue != null)
                go.transform.RotateAround(mr.bounds.center,Vector3.up,int.Parse(rotValue));
            if (go.name.Contains("safezone"))
            {
                MonoBehaviourPublicLi1ObsaInObUnique script1 = go.AddComponent<MonoBehaviourPublicLi1ObsaInObUnique>();
                MonoBehaviourPublicVoCoOnVoCoVoCoVoCoVo1 script2 = go.AddComponent<MonoBehaviourPublicVoCoOnVoCoVoCoVoCoVo1>();
                go.GetComponent<MeshCollider>().convex = true;
                go.GetComponent<MeshCollider>().isTrigger = true;
                go.layer = 13;
            }
            if (go.name.Contains("invis")) {
            mr.enabled = false;
        }
        return go;
	}

    public void SetMaterial(string name) {
		if (!_materialIndices.TryGetValue(name, out _currentIndexList))
		{
			_currentIndexList = new List<int>();
			_materialIndices[name] = _currentIndexList;
		}
	}


	public void PushFace(string material, List<int> vertexIndices, List<int> normalIndices, List<int> uvIndices) {
		//invalid face size?
		if (vertexIndices.Count < 3) {
			return;
		}

		//set material
		if (material != _lastMaterial) {
			SetMaterial(material);
			_lastMaterial = material;
		}

		//remap
		int[] indexRemap = new int[vertexIndices.Count];
		for (int i = 0; i < vertexIndices.Count; i++) {
			int vertexIndex = vertexIndices[i];
			int normalIndex = normalIndices[i];
			int uvIndex = uvIndices[i];

			var hashObj = new ObjLoopHash() {
				vertexIndex = vertexIndex,
				normalIndex = normalIndex,
				uvIndex = uvIndex
			};
			int remap = -1;

			if (!_globalIndexRemap.TryGetValue(hashObj, out remap)) {
				//add to dict
				_globalIndexRemap.Add(hashObj, _vertices.Count);
				remap = _vertices.Count;

				//add new verts and what not
				_vertices.Add((vertexIndex >= 0 && vertexIndex < _loader.Vertices.Count) ? _loader.Vertices[vertexIndex] : Vector3.zero);
				_normals.Add((normalIndex >= 0 && normalIndex < _loader.Normals.Count) ? _loader.Normals[normalIndex] : Vector3.zero);
				_uvs.Add((uvIndex >= 0 && uvIndex < _loader.UVs.Count) ? _loader.UVs[uvIndex] : Vector2.zero);

				//mark recalc flag
				if (normalIndex < 0)
					recalculateNormals = true;
			}

			indexRemap[i] = remap;
		}


		//add face to our mesh list
		if (indexRemap.Length == 3) {
			_currentIndexList.AddRange(new int[] { indexRemap[0], indexRemap[1], indexRemap[2] });
		} else if (indexRemap.Length == 4) {
			_currentIndexList.AddRange(new int[] { indexRemap[0], indexRemap[1], indexRemap[2] });
			_currentIndexList.AddRange(new int[] { indexRemap[2], indexRemap[3], indexRemap[0] });
		} else if (indexRemap.Length > 4) {
			for (int i = indexRemap.Length - 1; i >= 2; i--) {
				_currentIndexList.AddRange(new int[] { indexRemap[0], indexRemap[i - 1], indexRemap[i] });
			}
		}

		PushedFaceCount++;
	}

	public OBJObjectBuilder(string name, OBJLoader loader) {
		_name = name;
		_loader = loader;
	}
}
}