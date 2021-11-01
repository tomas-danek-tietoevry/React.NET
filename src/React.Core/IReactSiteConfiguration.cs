﻿/*
 *  Copyright (c) 2014-2015, Facebook, Inc.
 *  All rights reserved.
 *
 *  This source code is licensed under the BSD-style license found in the
 *  LICENSE file in the root directory of this source tree. An additional grant 
 *  of patent rights can be found in the PATENTS file in the same directory.
 */

using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace React
{
	/// <summary>
	/// Site-wide configuration for ReactJS.NET
	/// </summary>
	public interface IReactSiteConfiguration
	{
		/// <summary>
		/// Adds a script to the list of scripts that are executed. This is the same as
		/// <see cref="AddScript"/> except it does not run JSX transformation on the script and thus is
		/// more efficient.
		/// </summary>
		/// <param name="filename">
		/// Name of the file to execute. Should be a server relative path starting with ~ (eg. 
		/// <c>~/Scripts/Awesome.js</c>)
		/// </param>
		/// <returns>The configuration, for chaining</returns>
		IReactSiteConfiguration AddScriptWithoutTransform(string filename);

		/// <summary>
		/// Gets a list of all the scripts that have been added to this configuration and do not 
		/// require JSX transformation to be run.
		/// </summary>
		IEnumerable<string> ScriptsWithoutTransform { get; } 

		/// <summary>
		/// Gets or sets whether JavaScript engines should be reused across requests.
		/// </summary>
		/// 
		bool ReuseJavaScriptEngines { get; set; }
		/// <summary>
		/// Sets whether JavaScript engines should be reused across requests.
		/// </summary>
		IReactSiteConfiguration SetReuseJavaScriptEngines(bool value);

		/// <summary>
		/// Gets or sets the configuration for JSON serializer.
		/// </summary>
		JsonSerializerSettings JsonSerializerSettings { get; set; }

		/// <summary>
		/// Sets the configuration for json serializer.
		/// </summary>
		/// <remarks>
		/// This confiquration is used when component initialization script
		/// is being generated server-side.
		/// </remarks>
		/// <param name="settings">The settings.</param>
		IReactSiteConfiguration SetJsonSerializerSettings(JsonSerializerSettings settings);

		/// <summary>
		/// Gets or sets the number of engines to initially start when a pool is created. 
		/// Defaults to <c>10</c>.
		/// </summary>
		int? StartEngines { get; set; }
		/// <summary>
		/// Sets the number of engines to initially start when a pool is created. 
		/// Defaults to <c>10</c>.
		/// </summary>
		IReactSiteConfiguration SetStartEngines(int? startEngines);

		/// <summary>
		/// Gets or sets the maximum number of engines that will be created in the pool. 
		/// Defaults to <c>25</c>.
		/// </summary>
		int? MaxEngines { get; set; }
		/// <summary>
		/// Sets the maximum number of engines that will be created in the pool. 
		/// Defaults to <c>25</c>.
		/// </summary>
		IReactSiteConfiguration SetMaxEngines(int? maxEngines);
	}
}
