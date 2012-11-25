<?xml version="1.0" encoding="utf-8" ?>

<!--
/*
 * Copyright 2007 - 2009 Marek Stój
 * 
 * This file is part of ImmDoc .NET.
 *
 * ImmDoc .NET is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * ImmDoc .NET is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ImmDoc .NET; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
-->

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:output method="html"/>

  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template name="list" match="list">
    <xsl:choose>
      <xsl:when test="@type='number'">
        <xsl:apply-templates select="listheader" mode="BulletOrNumber"/>
        <ol>
          <xsl:apply-templates select="item" mode="BulletOrNumber"/>
        </ol>
      </xsl:when>
      <xsl:when test="@type='table'">
        <table class="MembersTable">
          <col width="30%" />
          <col width="70%" />
          <xsl:apply-templates select="listheader" mode="Table"/>
          <xsl:apply-templates select="item" mode="Table"/>
        </table>
      </xsl:when>
      <xsl:otherwise>
        <!-- Assuming typeDefinition = 'bullet' -->
        <xsl:apply-templates select="listheader" mode="BulletOrNumber"/>
        <ul>
          <xsl:apply-templates select="item" mode="BulletOrNumber"/>
        </ul>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="item" mode="BulletOrNumber">
    <li>
      <xsl:choose>
        <xsl:when test="term and description">
          <strong>
            <xsl:copy-of select="term/node()"/>
          </strong>
          <br/>
          <xsl:copy-of select="description/node()"/>
        </xsl:when>
        <xsl:when test="term">
          <xsl:copy-of select="term/node()"/>
        </xsl:when>
        <xsl:when test="description">
          <xsl:copy-of select="description/node()"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:text disable-output-escaping="yes">
            &amp;nbsp;
          </xsl:text>
        </xsl:otherwise>
      </xsl:choose>
    </li>
  </xsl:template>

  <xsl:template match="item" mode="Table">
    <tr>
      <td>
        <strong>
          <xsl:choose>
            <xsl:when test="term">
              <xsl:copy-of select="term/node()"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:text disable-output-escaping="yes">
                &amp;nbsp;
              </xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </strong>
      </td>
      <td>
        <xsl:choose>
          <xsl:when test="description">
            <xsl:for-each select="description/node()">
              <xsl:choose>
                <xsl:when test="local-name(current()) = 'list'">
                  <xsl:call-template name="list"/>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:copy-of select="current()"/>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:for-each>
          </xsl:when>
          <xsl:otherwise>
            <xsl:text disable-output-escaping="yes">
              &amp;nbsp;
            </xsl:text>
          </xsl:otherwise>
        </xsl:choose>
      </td>
    </tr>
  </xsl:template>

  <xsl:template match="listheader" mode="BulletOrNumber">
    <p>
      <xsl:choose>
        <xsl:when test="term and description">
          <xsl:copy-of select="term/node()"/>
          <xsl:copy-of select="description/node()"/>
        </xsl:when>
        <xsl:when test="term">
          <xsl:copy-of select="term/node()"/>
        </xsl:when>
        <xsl:when test="description">
          <xsl:copy-of select="description/node()"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:text disable-output-escaping="yes">
            &amp;nbsp;
          </xsl:text>
        </xsl:otherwise>
      </xsl:choose>
    </p>
  </xsl:template>

  <xsl:template match="listheader" mode="Table">
    <tr>
      <xsl:choose>
        <xsl:when test="term and description">
          <th>
            <xsl:copy-of select="term/node()"/>
          </th>
          <th>
            <xsl:copy-of select="description/node()"/>
          </th>
        </xsl:when>
        <xsl:when test="term">
          <th colspan="2">
            <xsl:copy-of select="term/node()"/>
          </th>
        </xsl:when>
        <xsl:when test="description">
          <th colspan="2">
            <xsl:copy-of select="description/node()"/>
          </th>
        </xsl:when>
        <xsl:otherwise>
          <th colspan="2">
            <xsl:text disable-output-escaping="yes">
              &amp;nbsp;
            </xsl:text>
          </th>
        </xsl:otherwise>
      </xsl:choose>
    </tr>
  </xsl:template>

</xsl:stylesheet>
