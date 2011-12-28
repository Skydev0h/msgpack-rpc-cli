#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using MsgPack.Rpc.Server.Dispatch.SvcFileInterop;

namespace MsgPack.Rpc.Server.Dispatch
{
	public class FileBasedServiceTypeLocator : ServiceTypeLocator
	{
		public string BaseDirectory { get; set; }

		public sealed override Collection<ServiceDescription> FindServices()
		{
			var result = new Collection<ServiceDescription>();
			foreach ( var file in Directory.GetFiles( String.IsNullOrWhiteSpace( this.BaseDirectory ) ? AppDomain.CurrentDomain.BaseDirectory : this.BaseDirectory, "*.svc" ) )
			{
				result.Add( ExtractServiceDescription( file ) );
			}

			return result;
		}

		private static ServiceDescription ExtractServiceDescription( string svcFile )
		{
			ServiceHostDirective directive;
			using ( var stream = new FileStream( svcFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan ) )
			using ( var reader = new StreamReader( stream ) )
			{
				directive = new SvcFileParser().Parse( reader );
			}

			var serviceType = Type.GetType( directive.Service, false );
			if ( serviceType == null )
			{
				throw new InvalidOperationException(
					String.Format(
						CultureInfo.CurrentCulture,
						"Cannot load service type '{0}' from directory '{1}'.",
						directive.Service,
						AppDomain.CurrentDomain.BaseDirectory
					)
				);
			}

			return ServiceDescription.FromServiceType( serviceType );
		}
	}
}