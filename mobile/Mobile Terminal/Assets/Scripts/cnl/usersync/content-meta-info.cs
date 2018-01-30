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
using ILOG.J2CsMapping.NIO;
using net.named_data.jndn;
using net.named_data.jndn.util;
using net.named_data.jndn.encoding.tlv;

namespace net.named_data.cnl_dot_net.usersync {
  /// <summary>
  /// ContentMetaInfo represents the information in the _meta packet of a
  /// Generalized Content.
  /// </summary>
  public class ContentMetaInfo {
    /// <summary>
    /// Create a ContentMetaInfo where all the fields have default unspecified 
    /// values.
    /// </summary>
    public ContentMetaInfo()
    {
      clear();
    }

    /// <summary>
    /// Create a ContentMetaInfo where all the fields are copied from the given
    /// object.
    /// </summary>
    /// <param name="contentMetaInfo">The other ContentMetaInfo to copy from.</param>
    public ContentMetaInfo(ContentMetaInfo contentMetaInfo)
    {
      contentType_ = contentMetaInfo.contentType_;
      timestamp_ = contentMetaInfo.timestamp_;
      hasSegments_ = contentMetaInfo.hasSegments_;
      other_ = contentMetaInfo.other_;
    }

    /// <summary>
    /// Get the content type.
    /// </summary>
    /// <returns>The content type. If not specified, return an empty string.</returns>
    public string
    getContentType() { return contentType_; }

    /// <summary>
    /// Get the time stamp.
    /// </summary>
    /// <returns>The time stamp as milliseconds since Jan 1, 1970 UTC. If not
    /// specified, return -1.</returns>
    public double
    getTimestamp() { return timestamp_; }

    /// <summary>
    /// Get the hasSegments flag.
    /// </summary>
    /// <returns>The hasSegments flag.</returns>
    public bool
    getHasSegments() { return hasSegments_; }

    /// <summary>
    /// Get the Blob containing the optional other info.
    /// </summary>
    /// <returns>The other info. If not specified, return an isNull Blob.</returns>
    public Blob
    getOther() { return other_; }

    /// <summary>
    /// Set the content type.
    /// </summary>
    /// <param name="contentType">The content type.</param>
    /// <returns>This ContentMetaInfo so that you can chain calls to update 
    /// values.</returns>
    public ContentMetaInfo
    setContentType(string contentType)
    {
      contentType_ = contentType;
      return this;
    }

    /// <summary>
    /// Set the time stamp.
    /// </summary>
    /// <param name="timestamp">The time stamp.</param>
    /// <returns>This ContentMetaInfo so that you can chain calls to update 
    /// values.</returns>
    public ContentMetaInfo
    setTimestamp(double timestamp)
    {
      timestamp_ = timestamp;
      return this;
    }

    /// <summary>
    /// Set the hasSegments flag.
    /// </summary>
    /// <param name="hasSegments">The hasSegments flag.</param>
    /// <returns>This ContentMetaInfo so that you can chain calls to update 
    /// values.</returns>
    public ContentMetaInfo
    setHasSegments(bool hasSegments)
    {
      hasSegments_ = hasSegments;
      return this;
    }

    /// <summary>
    /// Set the Blob containing the optional other info.
    /// </summary>
    /// <param name="other">The other info, or a default null Blob() if not 
    /// specified.</param>
    /// <returns>This ContentMetaInfo so that you can chain calls to update 
    /// values.</returns>
    public ContentMetaInfo
    setOther(Blob other)
    {
      other_ = other;
      return this;
    }

    /// <summary>
    /// Set all the fields to their default unspecified values.
    /// </summary>
    public void
    clear()
    {
      contentType_ = "";
      timestamp_ = -1;
      hasSegments_ = false;
      other_ = new Blob();
    }

    /// <summary>
    /// Encode this ContentMetaInfo.
    /// </summary>
    /// <returns>The encoding Blob.</returns>
    public Blob
    wireEncode()
    {
      TlvEncoder encoder = new TlvEncoder(256);
      int saveLength = encoder.getLength();

      // Encode backwards.
      if (!other_.isNull())
        encoder.writeBlobTlv(ContentMetaInfo_Other, other_.buf());
      if (hasSegments_)
        encoder.writeTypeAndLength(ContentMetaInfo_HasSegments, 0);
      encoder.writeNonNegativeIntegerTlv
        (ContentMetaInfo_Timestamp, (long)Math.Round(timestamp_));
      encoder.writeBlobTlv
        (ContentMetaInfo_ContentType, new Blob(contentType_).buf());
      
      encoder.writeTypeAndLength
        (ContentMetaInfo_ContentMetaInfo, encoder.getLength() - saveLength);
      return new Blob(encoder.getOutput(), false);
    }

    /// <summary>
    /// Decode the input and update this ContentMetaInfo.
    /// </summary>
    /// <param name="input">The input to decode.</param>
    public void
    wireDecode(ByteBuffer input)
    {
      clear();

      TlvDecoder decoder = new TlvDecoder(input);
      int endOffset = decoder.readNestedTlvsStart(ContentMetaInfo_ContentMetaInfo);

      // Set copy false since we just immediately get a string.
      Blob contentTypeText = new Blob
        (decoder.readBlobTlv(ContentMetaInfo_ContentType), false);
      contentType_ = contentTypeText.toString();
      timestamp_ = decoder.readNonNegativeIntegerTlv(ContentMetaInfo_Timestamp);
      hasSegments_ = decoder.readBooleanTlv
        (ContentMetaInfo_HasSegments, endOffset);
      if (decoder.peekType(ContentMetaInfo_Other, endOffset))
        other_ = new Blob(decoder.readBlobTlv(ContentMetaInfo_Other), true);

      decoder.finishNestedTlvs(endOffset);
    }

    /// <summary>
    /// Decode the input and update this ContentMetaInfo.
    /// </summary>
    /// <param name="input">The input to decode.</param>
    public void
    wireDecode(Blob input)
    {
      wireDecode(input.buf());
    }

    public const int ContentMetaInfo_ContentMetaInfo = 128;
    public const int ContentMetaInfo_ContentType = 129;
    public const int ContentMetaInfo_Timestamp = 130;
    public const int ContentMetaInfo_HasSegments = 131;
    public const int ContentMetaInfo_Other = 132;

    private string contentType_;
    private double timestamp_;
    private bool hasSegments_;
    private Blob other_;
  }
}
