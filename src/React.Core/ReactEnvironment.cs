/*
 *  Copyright (c) 2014-2015, Facebook, Inc.
 *  All rights reserved.
 *
 *  This source code is licensed under the BSD-style license found in the
 *  LICENSE file in the root directory of this source tree. An additional grant 
 *  of patent rights can be found in the PATENTS file in the same directory.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using JavaScriptEngineSwitcher.Core;

namespace React
{
	/// <summary>
	/// Request-specific ReactJS.NET environment. This is unique to the individual request and is 
	/// not shared.
	/// </summary>
	public class ReactEnvironment : IReactEnvironment, IDisposable
	{
		/// <summary>
		/// Format string used for React component container IDs
		/// </summary>
		protected const string CONTAINER_ELEMENT_NAME = "react{0}";

		/// <summary>
		/// JavaScript variable set when user-provided scripts have been loaded
		/// </summary>
		protected const string USER_SCRIPTS_LOADED_KEY = "_ReactNET_UserScripts_Loaded";
		/// <summary>
		/// Stack size to use for JSXTransformer if the default stack is insufficient
		/// </summary>
		protected const int LARGE_STACK_SIZE = 2 * 1024 * 1024;

		/// <summary>
		/// Factory to create JavaScript engines
		/// </summary>
		protected readonly IJavaScriptEngineFactory _engineFactory;
		/// <summary>
		/// Site-wide configuration
		/// </summary>
		protected readonly IReactSiteConfiguration _config;
		/// <summary>
		/// Cache used for storing compiled JSX
		/// </summary>
		protected readonly ICache _cache;
		/// <summary>
		/// File system wrapper
		/// </summary>
		protected readonly IFileSystem _fileSystem;
		/// <summary>
		/// Hash algorithm for file-based cache
		/// </summary>
		protected readonly IFileCacheHash _fileCacheHash;

		/// <summary>
		/// Version number of ReactJS.NET
		/// </summary>
		protected readonly Lazy<string> _version = new Lazy<string>(GetVersion);
		/// <summary>
		/// Contains an engine acquired from a pool of engines. Only used if 
		/// <see cref="IReactSiteConfiguration.ReuseJavaScriptEngines"/> is enabled.
		/// </summary>
		protected readonly Lazy<IJsEngine> _engineFromPool;

		/// <summary>
		/// Number of components instantiated in this environment
		/// </summary>
		protected int _maxContainerId = 0;
		/// <summary>
		/// List of all components instantiated in this environment
		/// </summary>
		protected readonly IList<IReactComponent> _components = new List<IReactComponent>();

		/// <summary>
		/// Gets the <see cref="IReactEnvironment"/> for the current request. If no environment
		/// has been created for the current request, creates a new one.
		/// </summary>
		public static IReactEnvironment Current
		{
			get { return AssemblyRegistration.Container.Resolve<IReactEnvironment>(); }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReactEnvironment"/> class.
		/// </summary>
		/// <param name="engineFactory">The JavaScript engine factory</param>
		/// <param name="config">The site-wide configuration</param>
		/// <param name="cache">The cache to use for JSX compilation</param>
		/// <param name="fileSystem">File system wrapper</param>
		/// <param name="fileCacheHash">Hash algorithm for file-based cache</param>
		public ReactEnvironment(
			IJavaScriptEngineFactory engineFactory,
			IReactSiteConfiguration config,
			ICache cache,
			IFileSystem fileSystem,
			IFileCacheHash fileCacheHash
		)
		{
			_engineFactory = engineFactory;
			_config = config;
			_cache = cache;
			_fileSystem = fileSystem;
			_fileCacheHash = fileCacheHash;
			_engineFromPool = new Lazy<IJsEngine>(() => _engineFactory.GetEngine());
		}

		/// <summary>
		/// Gets the JavaScript engine to use for this environment.
		/// </summary>
		protected virtual IJsEngine Engine
		{
			get
			{
				return _config.ReuseJavaScriptEngines
					? _engineFromPool.Value
					: _engineFactory.GetEngineForCurrentThread();
			}
		}

		/// <summary>
		/// Gets the version of the JavaScript engine in use by ReactJS.NET
		/// </summary>
		public virtual string EngineVersion
		{
			get { return Engine.Name + " " + Engine.Version; }
		}

		/// <summary>
		/// Gets the version number of ReactJS.NET
		/// </summary>
		public virtual string Version
		{
			get { return _version.Value; }
		}

		/// <summary>
		/// Executes the provided JavaScript code.
		/// </summary>
		/// <param name="code">JavaScript to execute</param>
		public virtual void Execute(string code)
		{
			try
			{
				Engine.Execute(code);
			}
			catch (JsRuntimeException ex)
			{
				throw WrapJavaScriptRuntimeException(ex);
			}
		}

		/// <summary>
		/// Executes the provided JavaScript code, returning a result of the specified type.
		/// </summary>
		/// <typeparam name="T">Type to return</typeparam>
		/// <param name="code">Code to execute</param>
		/// <returns>Result of the JavaScript code</returns>
		public virtual T Execute<T>(string code)
		{
			try
			{
				return Engine.Evaluate<T>(code);
			}
			catch (JsRuntimeException ex)
			{
				throw WrapJavaScriptRuntimeException(ex);
			}
		}

		/// <summary>
		/// Executes the provided JavaScript function, returning a result of the specified type.
		/// </summary>
		/// <typeparam name="T">Type to return</typeparam>
		/// <param name="function">JavaScript function to execute</param>
		/// <param name="args">Arguments to pass to function</param>
		/// <returns>Result of the JavaScript code</returns>
		public virtual T Execute<T>(string function, params object[] args)
		{
			try
			{
				return Engine.CallFunctionReturningJson<T>(function, args);
			}
			catch (JsRuntimeException ex)
			{
				throw WrapJavaScriptRuntimeException(ex);
			}
		}

		/// <summary>
		/// Determines if the specified variable exists in the JavaScript engine
		/// </summary>
		/// <param name="name">Name of the variable</param>
		/// <returns><c>true</c> if the variable exists; <c>false</c> otherwise</returns>
		public virtual bool HasVariable(string name)
		{
			try
			{
				return Engine.HasVariable(name);
			}
			catch (JsRuntimeException ex)
			{
				throw WrapJavaScriptRuntimeException(ex);
			}
		}

		/// <summary>
		/// Creates an instance of the specified React JavaScript component.
		/// </summary>
		/// <typeparam name="T">Type of the props</typeparam>
		/// <param name="componentName">Name of the component</param>
		/// <param name="props">Props to use</param>
		/// <param name="containerId">ID to use for the container HTML tag. Defaults to an auto-generated ID</param>
		/// <returns>The component</returns>
		public virtual IReactComponent CreateComponent<T>(string componentName, T props, string containerId = null)
		{
			if (string.IsNullOrEmpty(containerId))
			{
				_maxContainerId++;
				containerId = string.Format(CONTAINER_ELEMENT_NAME, _maxContainerId);	
			}
			
			var component = new ReactComponent(this, _config, componentName, containerId)
			{
				Props = props
			};
			_components.Add(component);
			return component;
		}

		/// <summary>
		/// Renders the JavaScript required to initialise all components client-side. This will 
		/// attach event handlers to the server-rendered HTML.
		/// </summary>
		/// <returns>JavaScript for all components</returns>
		public virtual string GetInitJavaScript()
		{
			var fullScript = new StringBuilder();
			
			// Propagate any server-side console.log calls to corresponding client-side calls.
			var consoleCalls = Execute<string>("console.getCalls()");
			fullScript.Append(consoleCalls);
			
			foreach (var component in _components)
			{
				fullScript.Append(component.RenderJavaScript());
				fullScript.AppendLine(";");
			}

			return fullScript.ToString();
		}

		/// <summary>
		/// Gets the ReactJS.NET version number. Use <see cref="Version" /> instead.
		/// </summary>
		private static string GetVersion()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var rawVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
			var lastDot = rawVersion.LastIndexOf('.');
			var version = rawVersion.Substring(0, lastDot);
			var build = rawVersion.Substring(lastDot + 1);
			return string.Format("{0} (build {1})", version, build);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public virtual void Dispose()
		{
			_engineFactory.DisposeEngineForCurrentThread();
			if (_engineFromPool.IsValueCreated)
			{
				_engineFactory.ReturnEngineToPool(_engineFromPool.Value);
			}
		}

		/// <summary>
		/// Updates the Message of a <see cref="JsRuntimeException"/> to be more useful, containing
		/// the line and column numbers.
		/// </summary>
		/// <param name="ex">Original exception</param>
		/// <returns>New exception</returns>
		protected virtual JsRuntimeException WrapJavaScriptRuntimeException(JsRuntimeException ex)
		{
			return new JsRuntimeException(string.Format(
				"{0}\r\nLine: {1}\r\nColumn:{2}",
				ex.Message,
				ex.LineNumber,
				ex.ColumnNumber
			), ex.EngineName, ex.EngineVersion)
			{
				ErrorCode = ex.ErrorCode,
				Category = ex.Category,
				LineNumber = ex.LineNumber,
				ColumnNumber = ex.ColumnNumber,
				SourceFragment = ex.SourceFragment,
				Source = ex.Source,
			};
		}

		/// <summary>
		/// Gets the site-wide configuration.
		/// </summary>
		public virtual IReactSiteConfiguration Configuration
		{
			get { return _config; }
		}
	}
}
