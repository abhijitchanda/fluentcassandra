﻿using System;
using System.Collections.Generic;
using System.Linq;
using Apache.Cassandra;
using FluentCassandra.Connections;
using FluentCassandra.Linq;
using FluentCassandra.Operations;
using FluentCassandra.Types;

namespace FluentCassandra
{
	public class CassandraContext : IDisposable
	{
		private readonly IList<IFluentMutationTracker> _trackers;
		private readonly IConnectionBuilder _connectionBuilder;
		private CassandraSession _session;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="keyspace"></param>
		/// <param name="server"></param>
		/// <param name="timeout"></param>
		public CassandraContext(string keyspace, Server server)
			: this(keyspace, server.Host, server.Port, server.Timeout) { }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="keyspace"></param>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="timeout"></param>
		/// <param name="provider"></param>
		public CassandraContext(string keyspace, string host, int port = Server.DefaultPort, int timeout = Server.DefaultTimeout)
			: this(new ConnectionBuilder(keyspace, host, port, timeout)) { }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="connectionString"></param>
		public CassandraContext(string connectionString)
			: this(new ConnectionBuilder(connectionString)) { }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="session"></param>
		public CassandraContext(CassandraSession session)
			: this(session.ConnectionBuilder)
		{
			_session = session;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="connectionBuilder"></param>
		public CassandraContext(IConnectionBuilder connectionBuilder)
		{
			ThrowErrors = true;

			_trackers = new List<IFluentMutationTracker>();
			_connectionBuilder = connectionBuilder;

			Keyspace = new CassandraKeyspace(_connectionBuilder.Keyspace, this);
		}

		/// <summary>
		/// Gets a typed column family.
		/// </summary>
		/// <typeparam name="CompareWith"></typeparam>
		/// <returns></returns>
		public CassandraColumnFamily GetColumnFamily(string columnFamily)
		{
			return new CassandraColumnFamily(this, columnFamily);
		}

		/// <summary>
		/// Gets a typed column family.
		/// </summary>
		/// <typeparam name="CompareWith"></typeparam>
		/// <returns></returns>
		public CassandraSuperColumnFamily GetSuperColumnFamily(string columnFamily)
		{
			return new CassandraSuperColumnFamily(this, columnFamily);
		}

		/// <summary>
		/// Gets a typed super column family.
		/// </summary>
		/// <typeparam name="CompareWith"></typeparam>
		/// <typeparam name="CompareSubcolumnWith"></typeparam>
		/// <param name="columnFamily"></param>
		/// <returns></returns>
		[Obsolete("Use \"GetColumnFamily\" with out generic parameters")]
		public CassandraColumnFamily<CompareWith> GetColumnFamily<CompareWith>(string columnFamily)
			where CompareWith : CassandraObject
		{
			return new CassandraColumnFamily<CompareWith>(this, columnFamily);
		}

		/// <summary>
		/// Gets a typed super column family.
		/// </summary>
		/// <typeparam name="CompareWith"></typeparam>
		/// <typeparam name="CompareSubcolumnWith"></typeparam>
		/// <param name="columnFamily"></param>
		/// <returns></returns>
		[Obsolete("Use \"GetSuperColumnFamily\" with out generic parameters")]
		public CassandraSuperColumnFamily<CompareWith, CompareSubcolumnWith> GetColumnFamily<CompareWith, CompareSubcolumnWith>(string columnFamily)
			where CompareWith : CassandraObject
			where CompareSubcolumnWith : CassandraObject
		{
			return new CassandraSuperColumnFamily<CompareWith, CompareSubcolumnWith>(this, columnFamily);
		}

		/// <summary>
		/// 
		/// </summary>
		public CassandraKeyspace Keyspace { get; set; }

		#region Cassandra System For Server

		public string AddKeyspace(KsDef definition)
		{
			return ExecuteOperation(new SimpleOperation<string>(ctx => {
				return ctx.Session.GetClient(setKeyspace: false).system_add_keyspace(definition);
			}));
		}

		public string UpdateKeyspace(KsDef definition)
		{
			return ExecuteOperation(new SimpleOperation<string>(ctx => {
				return ctx.Session.GetClient(setKeyspace: false).system_update_keyspace(definition);
			}));
		}

		public string DropKeyspace(string keyspace)
		{
			return ExecuteOperation(new SimpleOperation<string>(ctx => {
				return ctx.Session.GetClient(setKeyspace: false).system_drop_keyspace(keyspace);
			}));
		}

		public string AddColumnFamily(CfDef definition)
		{
			Keyspace.ClearCachedKeyspaceSchema();

			return ExecuteOperation(new SimpleOperation<string>(ctx => {
				return ctx.Session.GetClient().system_add_column_family(definition);
			}));
		}

		public string UpdateColumnFamily(CfDef definition)
		{
			Keyspace.ClearCachedKeyspaceSchema();

			return ExecuteOperation(new SimpleOperation<string>(ctx => {
				return ctx.Session.GetClient().system_update_column_family(definition);
			}));
		}

		public string DropColumnFamily(string columnFamily)
		{
			Keyspace.ClearCachedKeyspaceSchema();

			return ExecuteOperation(new SimpleOperation<string>(ctx => {
				return ctx.Session.GetClient().system_drop_column_family(columnFamily);
			}));
		}

		#endregion

		#region Cassandra Descriptions For Server

		public bool KeyspaceExists(string keyspaceName)
		{
			return DescribeKeyspaces().Any(keyspace => keyspace.KeyspaceName == keyspaceName);
		}

		public IEnumerable<CassandraKeyspace> DescribeKeyspaces()
		{
			return ExecuteOperation(new SimpleOperation<IEnumerable<CassandraKeyspace>>(ctx => {
				IEnumerable<KsDef> keyspaces = ctx.Session.GetClient(setKeyspace: false).describe_keyspaces();
				return keyspaces.Select(keyspace => new CassandraKeyspace(new CassandraKeyspaceSchema(keyspace), this));
			}));
		}

		public string DescribeClusterName()
		{
			return ExecuteOperation(new SimpleOperation<string>(ctx => {
				return ctx.Session.GetClient(setKeyspace: false).describe_cluster_name();
			}));
		}

		public Dictionary<string, List<string>> DescribeSchemaVersions()
		{
			return ExecuteOperation(new SimpleOperation<Dictionary<string, List<string>>>(ctx => {
				return ctx.Session.GetClient(setKeyspace: false).describe_schema_versions();
			}));
		}

		public string DescribeVersion()
		{
			return ExecuteOperation(new SimpleOperation<string>(ctx => {
				return ctx.Session.GetClient(setKeyspace: false).describe_version();
			}));
		}

		public string DescribePartitioner()
		{
			return ExecuteOperation(new SimpleOperation<string>(ctx => {
				return ctx.Session.GetClient(setKeyspace: false).describe_partitioner();
			}));
		}

		public string DescribeSnitch()
		{
			return ExecuteOperation(new SimpleOperation<string>(ctx => {
				return ctx.Session.GetClient(setKeyspace: false).describe_snitch();
			}));
		}

		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="record"></param>
		public void Attach(IFluentRecord record)
		{
			var tracker = record.MutationTracker;

			if (_trackers.Contains(tracker))
				return;

			_trackers.Add(tracker);
		}

		/// <summary>
		/// Saves the pending changes.
		/// </summary>
		public void SaveChanges()
		{
			lock (_trackers)
			{
				var mutations = new List<FluentMutation>();

				foreach (var tracker in _trackers)
					mutations.AddRange(tracker.GetMutations());

				var op = new BatchMutate(mutations);
				ExecuteOperation(op);

				foreach (var tracker in _trackers)
					tracker.Clear();

				_trackers.Clear();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="record"></param>
		public void SaveChanges(IFluentRecord record)
		{
			var mutations = record.MutationTracker.GetMutations();
			var op = new BatchMutate(mutations);
			ExecuteOperation(op);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cqlQuery"></param>
		public IEnumerable<ICqlRow> ExecuteQuery(UTF8Type cqlQuery)
		{
			var op = new ExecuteCqlQuery(cqlQuery);
			return ExecuteOperation(op);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cqlQuery"></param>
		public void ExecuteNonQuery(UTF8Type cqlQuery)
		{
			var op = new ExecuteCqlNonQuery(cqlQuery);
			ExecuteOperation(op);
		}

		/// <summary>
		/// The last error that occured during the execution of an operation.
		/// </summary>
		public CassandraException LastError { get; private set; }

		/// <summary>
		/// Indicates if errors should be thrown when occuring on opperation.
		/// </summary>
		public bool ThrowErrors { get; set; }

		/// <summary>
		/// Execute the column family operation against the connection to the server.
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="action"></param>
		/// <param name="throwOnError"></param>
		/// <returns></returns>
		public TResult ExecuteOperation<TResult>(Operation<TResult> action, bool? throwOnError = null)
		{
			if (WasDisposed)
				throw new ObjectDisposedException(GetType().FullName);

			var localSession = _session == null;
			var session = _session;
			if (session == null)
				session = new CassandraSession(_connectionBuilder);

			action.Context = this;

			try
			{
				var result = session.ExecuteOperation(action, throwOnError ?? ThrowErrors);
				LastError = session.LastError;

				return result;
			}
			finally
			{
				if (localSession && session != null)
					session.Dispose();
			}
		}

		#region IDisposable Members

		/// <summary>
		/// 
		/// </summary>
		public bool WasDisposed
		{
			get;
			private set;
		}

		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// The dispose.
		/// </summary>
		/// <param name="disposing">
		/// The disposing.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (!WasDisposed && disposing && _session != null)
			{
				_session.Dispose();
				_session = null;
			}

			WasDisposed = true;
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="Mongo"/> class. 
		/// </summary>
		~CassandraContext()
		{
			Dispose(false);
		}

		#endregion
	}
}