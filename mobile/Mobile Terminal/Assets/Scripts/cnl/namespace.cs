/**
 * Copyright (C) 2017-2018 Regents of the University of California.
 * @author: Jeff Thompson <jefft0@remap.ucla.edu>
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

using System;
using System.Collections.Generic;
using ILOG.J2CsMapping.Util.Logging;
using net.named_data.jndn;
using net.named_data.jndn.util;

namespace net.named_data.cnl_dot_net {
  public delegate void OnNameAdded
    (Namespace nameSpace, Namespace addedNamespace, long callbackId);

  public delegate void OnContentSet
    (Namespace nameSpace, Namespace contentNamespace, long callbackId);

  public delegate void OnContentTransformed(Data data, object content);

  public delegate void TransformContent
    (Data data, OnContentTransformed onContentTransformed);

  /// <summary>
  /// Namespace is the main class that represents the name tree and related
  /// operations to manage it.
  /// </summary>
  public class Namespace : OnData {
    /// <summary>
    /// Create a Namespace object with the given name, and with no parent. This
    /// is the root of the name tree. To create child nodes, use
    /// myNamespace.getChild("foo") or myNamespace["foo"].
    /// </summary>
    /// <param name="name">The name of this root node in the namespace.
    /// This makes a copy of the name.</param>
    public Namespace(Name name)
    {
      name_ = new Name(name);

      defaultInterestTemplate_ = new Interest();
      defaultInterestTemplate_.setInterestLifetimeMilliseconds(4000.0);
    }

    /// <summary>
    /// Create a Namespace object with the given name, and with no parent. This
    /// is the root of the name tree. To create child nodes, use
    /// myNamespace.getChild("foo") or myNamespace["foo"].
    /// </summary>
    /// <param name="uri">The name URI string.</param>
    public Namespace(string uri) : this(new Name(uri)) {}

    /// <summary>
    /// Get the name of this node in the name tree. This includes the name
    /// components of parent nodes. To get the name component of just this node,
    /// use getName()[-1].
    /// </summary>
    /// <returns>The name of this namespace. NOTE: You must not change the name.
    /// If you need to change it then make a copy.</returns>
    public Name
    getName() { return name_; }

    /// <summary>
    /// Get the parent namespace.
    /// </summary>
    /// <returns>The parent namespace, or null if this is the root of the tree.
    /// </returns>
    public Namespace
    getParent() { return parent_; }

    /// <summary>
    /// Get the root namespace (which has no parent node).
    /// </summary>
    /// <returns>The root namespace.</returns>
    public Namespace
    getRoot()
    {
      var result = this;
      while (result.parent_ != null)
        result = result.parent_;
      return result;
    }

    /// <summary>
    /// Check if this node in the namespace has the given child.
    /// </summary>
    /// <param name="component">The name component of the child.</param>
    /// <returns>True if this has a child with the name component.</returns>
    public bool
    hasChild(Name.Component component)
    {
      return children_.ContainsKey(component);
    }

    /// <summary>
    /// Get a child, creating it if needed. This is equivalent to
    /// nameSpace[component]. If a child is created, this calls callbacks as
    /// described by addOnNameAdded.
    /// </summary>
    /// <param name="component"> The name component of the immediate child.
    /// </param>
    /// <returns>The child Namespace object.</returns>
    public Namespace
    getChild(Name.Component component)
    {
      Namespace child;
      if (children_.TryGetValue(component, out child))
        return child;
      else
        return createChild(component, true);
    }

    /// <summary>
    /// A helper method to return getChild(new Name.Component(value)).
    /// </summary>
    public Namespace
    getChild(String value) { return getChild(new Name.Component(value)); }

    /// <summary>
    /// A helper method to return getChild(new Name.Component(value)).
    /// </summary>
    public Namespace
    getChild(byte[] value) { return getChild(new Name.Component(value)); }

    /// <summary>
    /// A helper method to return getChild(new Name.Component(value)).
    /// </summary>
    public Namespace
    getChild(Blob value) { return getChild(new Name.Component(value)); }
      
    /// <summary>
    /// Get a child (or descendant), creating it if needed. This is equivalent
    /// to namespace[descendantName]. If a child is created, this calls
    /// callbacks as described by addOnNameAdded (but does not call the
    /// callbacks when creating intermediate nodes).
    /// </summary>
    /// <param name="descendantName">Find or create the descendant node with the
    /// Name (which must have this node's name as a prefix).</param>
    /// <returns>The child Namespace object. However, if name equals the name of
    /// this Namespace, then just return this Namespace.</returns>
    /// <exception cref="Exception">If the name of this Namespace node is not a
    /// prefix of the given Name.</exception>
    public Namespace
    getChild(Name descendantName)
    {
      if (!name_.isPrefixOf(descendantName))
        throw new Exception
          ("The name of this node is not a prefix of the descendant name");

      // Find or create the child node whose name equals the descendantName.
      // We know descendantNamespace is a prefix, so we can just go by
      // component count instead of a full compare.
      var descendantNamespace = this;
      while (descendantNamespace.name_.size() < descendantName.size()) {
        var nextComponent = descendantName[descendantNamespace.name_.size()];

        Namespace child;
        if (descendantNamespace.children_.TryGetValue(nextComponent, out child))
          descendantNamespace = child;
        else {
          // Only fire the callbacks for the leaf node.
          var isLeaf =
            (descendantNamespace.name_.size() == descendantName.size() - 1);
          descendantNamespace = descendantNamespace.createChild
            (nextComponent, isLeaf);
        }
      }

      return descendantNamespace;
    }

    /// <summary>
    /// Return getChild(component).
    /// </summary>
    public Namespace
    this[Name.Component component] { get { return getChild(component); } }

    /// <summary>
    /// Return getChild(value).
    /// </summary>
    public Namespace
    this[String value] { get { return getChild(value); } }

    /// <summary>
    /// Return getChild(value).
    /// </summary>
    public Namespace
    this[byte[] value] { get { return getChild(value); } }

    /// <summary>
    /// Return getChild(value).
    /// </summary>
    public Namespace
    this[Blob value] { get { return getChild(value); } }

    /// <summary>
    /// Return getChild(descendantName).
    /// </summary>
    public Namespace
    this[Name descendantName] { get { return getChild(descendantName); } }

    /// <summary>
    /// Get a list of the name component of all child nodes.
    /// </summary>
    /// <returns>A fresh sorted list of the name component of all child nodes.
    /// This remains the same if child nodes are added or deleted.</returns>
    public ArrayList<Name.Component>
    getChildComponents()
    {
      var result = new ArrayList<Name.Component>();
      foreach (Name.Component child in children_.Keys)
        result.Add(child);

      return result;
    }

    /// <summary>
    /// Attach the Data packet to this Namespace. This calls callbacks as
    /// described by addOnContentSet. If a Data packet is already attached, do
    /// nothing.
    /// </summary>
    /// <param name="data">The Data packet object whose name must equal the name
    /// in this Namespace node. To get the right Namespace, you can use
    /// getChild(data.getName()). For efficiency, this does not copy the Data
    /// packet object. If your application may change the object later, then you
    /// must call setData with a copy of the object.</param>
    /// <exception cref="Exception">If the Data packet name does not equal the
    /// name of this Namespace node.</exception>
    public void
    setData(Data data)
    {
      if (data_ != null)
        // We already have an attached object.
        return;
      if (!data.getName().equals(name_))
        throw new Exception
          ("The Data packet name does not equal the name of this Namespace node");

      var transformContent = getTransformContent();
      // TODO: TransformContent should take an OnError.
      if (transformContent != null)
        transformContent(data, onContentTransformed);
      else
        // Otherwise just invoke directly.
        onContentTransformed(data, data.getContent());
    }

    /// <summary>
    /// Get the Data packet attached to this Namespace object. Note that
    /// getContent() may be different than the content in the attached Data
    /// packet (for example if the content is decrypted). To get the content,
    /// you should use getContent() instead of getData().getContent(). Also, the
    /// Data packet name is the same as the name of this Namespace node, so you
    /// can simply use getName() instead of getData().getName(). You should only
    /// use getData() to get other information such as the MetaInfo.
    /// </summary>
    /// <returns>The Data packet object, or null if not set.</returns>
    public Data
    getData() { return data_; }

    /// <summary>
    /// Get the content attached to this Namespace object. Note that
    /// getContent() may be different than the content in the attached Data
    /// packet (for example if the content is decrypted). In the default
    /// behavior, the content is the Blob content of the Data packet, but may be 
    /// a different type as determined by the attached handler.
    /// </summary>
    /// <returns>The content which is a Blob or other type as determined by the
    /// attached handler. You must cast to the correct type. If the content is
    /// not set, return null.</returns>
    public object
    getContent() { return content_; }

    /// <summary>
    /// Add an OnNameAdded callback. When a new name is added to this namespace
    /// at this node or any children, this calls onNameAdded as described below.
    /// </summary>
    /// <param name="onNameAdded">This calls
    /// onNameAdded(nameSpace, addedNamespace, callbackId) where nameSpace is
    /// this Namespace, addedNamespace is the Namespace of the added name, and
    /// callbackId is the callback ID returned by this method. NOTE: The library
    /// will log any exceptions thrown by this callback, but for better error
    /// handling the callback should catch and properly handle any exceptions.
    /// </param>
    /// <returns>The callback ID which you can use in removeCallback().</returns>
    public long
    addOnNameAdded(OnNameAdded onNameAdded)
    {
      var callbackId = getNextCallbackId();
      onNameAddedCallbacks_[callbackId] = onNameAdded;
      return callbackId;
    }

    /// <summary>
    /// Add an OnContentSet callback. When the content has been set for this
    /// Namespace node or any children, this calls onContentSet as described
    /// below.
    /// </summary>
    /// <param name="onContentSet">This calls
    /// onContentSet(nameSpace, contentNamespace, callbackId) where nameSpace is
    /// this Namespace, contentNamespace is the Namespace where the content was
    /// set, and callbackId is the callback ID returned by this method. If you
    /// only care if the content has been set for this Namespace (and not any of
    /// its children) then your callback can check
    /// "if contentNamespace == nameSpace". To get the content or data packet,
    /// use contentNamespace.getContent() or contentNamespace.getData(). NOTE:
    /// The library will log any exceptions thrown by this callback, but for
    /// better error handling the callback should catch and properly handle any
    /// exceptions.</param>
    /// <returns>The callback ID which you can use in removeCallback().</returns>
    public long
    addOnContentSet(OnContentSet onContentSet)
    {
      var callbackId = getNextCallbackId();
      onContentSetCallbacks_[callbackId] = onContentSet;
      return callbackId;
    }

    /// <summary>
    /// Set the Face used when expressInterest is called on this or child nodes
    /// (unless a child node has a different Face).
    /// TODO: Replace this by a mechanism for requesting a Data object which is
    /// more general than a Face network operation.
    /// </summary>
    /// <param name="face">The Face object. If this Namespace object already has
    /// a Face object, it is replaced.</param>
    public void
    setFace(Face face) { face_ = face; }

    /// <summary>
    /// Call expressInterest on this (or a parent's) Face where the interest
    /// name is the name of this Namespace node. When the Data packet is
    /// received this calls setData, so you should use a callback with
    /// addOnContentSet. This uses ExponentialReExpress to re-express a
    /// timed-out interest with longer lifetimes.
    /// TODO: How to alert the application on a final interest timeout?
    /// TODO: Replace this by a mechanism for requesting a Data object which is
    /// more general than a Face network operation.
    /// </summary>
    /// <param name="interestTemplate">(optional) The interest template for
    /// expressInterest. If omitted, just use a default interest lifetime.
    /// </param>
    /// <exception cref="Exception">If a Face object has not been set for this
    /// or a parent Namespace node.</exception>
    public void
    expressInterest(Interest interestTemplate = null)
    {
      var face = getFace();
      if (face == null)
        throw new Exception
          ("A Face object has not been set for this or a parent");

      if (interestTemplate == null)
        interestTemplate = defaultInterestTemplate_;
      logger_.log(Level.FINE, "Namespace: Express interest " + name_.toUri());
      face.expressInterest
        (name_, interestTemplate, this,
         ExponentialReExpress.makeOnTimeout(face, this, null));
    }

    /// <summary>
    /// Remove the callback with the given callbackId. This does not search for
    /// the callbackId in child nodes. If the callbackId isn't found, do nothing.
    /// </summary>
    /// <param name="callbackId">The callback ID returned, for example, from
    /// addOnNameAdded.</param>
    public void
    removeCallback(long callbackId)
    {
      onNameAddedCallbacks_.Remove(callbackId);
      onContentSetCallbacks_.Remove(callbackId);
    }

    /// <summary>
    /// Get the next unique callback ID. This uses a lock to be thread safe.
    /// This is an internal method only meant to be called by library classes;
    /// the application should not call it.
    /// </summary>
    /// <returns>The next callback ID.</returns>
    public static long
    getNextCallbackId()
    {
      lock (lastCallbackIdLock_) {
        return ++lastCallbackId_;
      }
    }

    /// <summary>
    /// Get the Face set by setFace on this or a parent Namespace node.
    /// </summary>
    /// <returns>The Face, or null if not set on this or any parent.</returns>
    private Face
    getFace()
    {
      var nameSpace = this;
      while (nameSpace != null) {
        if (nameSpace.face_ != null)
          return nameSpace.face_;
        nameSpace = nameSpace.parent_;
      }

      return null;
    }

    /// <summary>
    /// Get the TransformContent callback on this or a parent Namespace node.
    /// </summary>
    /// <returns>The TransformContent callback, or null if not set on this or
    /// any parent.</returns>
    private TransformContent
    getTransformContent()
    {
      var nameSpace = this;
      while (nameSpace != null) {
        if (nameSpace.transformContent_ != null)
          return nameSpace.transformContent_;
        nameSpace = nameSpace.parent_;
      }

      return null;
    }

    /// <summary>
    /// Create the child with the given name component and add it to this
    /// namespace. This private method should only be called if the child does
    /// not already exist. The application should use getChild.
    /// </summary>
    /// <param name="component">The name component of the child.</param>
    /// <param name="fireCallbacks">If true, call fireOnNameAdded for this and
    /// all parent nodes. If false, don't call callbacks (for example if
    /// creating intermediate nodes).</param>
    /// <returns>The child Namespace object.</returns>
    private Namespace
    createChild(Name.Component component, bool fireCallbacks)
    {
      var child = new Namespace(new Name(name_).append(component));
      child.parent_ = this;
      children_[component] = child;

      if (fireCallbacks) {
        var nameSpace = this;
        while (nameSpace != null) {
          nameSpace.fireOnNameAdded(child);
          nameSpace = nameSpace.parent_;
        }
      }

      return child;
    }

    private void
    fireOnNameAdded(Namespace addedNamespace)
    {
      // Copy the keys before iterating since callbacks can change the list.
      var keys = new long[onNameAddedCallbacks_.Count];
      onNameAddedCallbacks_.Keys.CopyTo(keys, 0);

      foreach (long key in keys) {
        // A callback on a previous pass may have removed this callback, so check.
        OnNameAdded onNameAdded;
        if (onNameAddedCallbacks_.TryGetValue(key, out onNameAdded)) {
          // TODO: Log exceptions.
          onNameAdded(this, addedNamespace, key);
        }
      }
    }

    /// <summary>
    /// Set data_ and content_ to the given values and fire the OnContentSet
    /// callbacks. This may be called from a transformContent_ handler invoked
    /// by setData.
    /// </summary>
    /// <param name="data">The Data packet object given to setData.</param>
    /// <param name="content"The content which may have been processed from the
    /// Data packet, e.g. by decrypting.</param>
    private void
    onContentTransformed(Data data, object content)
    {
      data_ = data;
      content_ = content;

      // Fire callbacks.
      var nameSpace = this;
      while (nameSpace != null) {
        nameSpace.fireOnContentSet(this);
        nameSpace = nameSpace.parent_;
      }
    }

    private void
    fireOnContentSet(Namespace contentNamespace)
    {
      // Copy the keys before iterating since callbacks can change the list.
      var keys = new long[onContentSetCallbacks_.Count];
      onContentSetCallbacks_.Keys.CopyTo(keys, 0);

      foreach (long key in keys) {
        // A callback on a previous pass may have removed this callback, so check.
        OnContentSet onContentSet;
        if (onContentSetCallbacks_.TryGetValue(key, out onContentSet)) {
          // TODO: Log exceptions.
          onContentSet(this, contentNamespace, key);
        }
      }
    }

    public void
    onData(Interest interest, Data data)
    {
      getChild(data.getName()).setData(data);
    }

    public void
    debugOnContentTransformed(Data data, object content)
    {
      onContentTransformed(data, content);
    }

    private Name name_;
    private Namespace parent_ = null;
    // The key is a Name.Component. The value is the child Namespace.
    private Dictionary<Name.Component, Namespace> children_ = new
      Dictionary<Name.Component, Namespace>();
    private Data data_ = null;
    private object content_ = null;
    private Face face_ = null;
    // The key is the callback ID. The value is the OnNameAdded function.
    private Dictionary<long, OnNameAdded> onNameAddedCallbacks_ =
      new Dictionary<long, OnNameAdded>();
    // The key is the callback ID. The value is the OnContentSet function.
    private Dictionary<long, OnContentSet> onContentSetCallbacks_ =
      new Dictionary<long, OnContentSet>();
    public TransformContent transformContent_ = null;
    private Interest defaultInterestTemplate_;
    private static long lastCallbackId_ = 0;
    private static object lastCallbackIdLock_ = new object();
    public bool debugSegmentStreamDidExpressInterest_ = false;
    private static Logger logger_ = ILOG.J2CsMapping.Util.Logging.Logger
      .getLogger(typeof(Namespace).FullName);
  }
}
