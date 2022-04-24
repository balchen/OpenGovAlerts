<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" version="4.0" encoding="UTF-8" doctype-public="-//W3C//DTD XHTML 1.0 Transitional//EN" doctype-system="http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd" omit-xml-declaration="yes" indent="no"/>

  <xsl:template match="notification">
    <html xmlns="http://www.w3.org/1999/xhtml">
      <head>
        <title>
          Nye møter funnet
        </title>
        <style type="text/css">
        </style>
      </head>
      <body>
        <h3>Nye møter har dukket opp på kalenderen</h3>
        <table>
          <xsl:apply-templates select="/notification/search"/>
        </table>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="search">
    <tr class="search">
      <td class="name">
        <xsl:value-of select="./name"/>
      </td>
      <td class="phrase">
        <xsl:value-of select="./phrase"/>
      </td>
    </tr>
    <xsl:apply-templates select="./meeting"/>
  </xsl:template>

  <xsl:template match="meeting">
    <tr class="meeting">
      <td>
        <a>
          <xsl:attribute name="href">
            <xsl:value-of select="./url"/>
          </xsl:attribute>
          <xsl:value-of select="./title"/>
        </a>
      </td>
      <td>
        <a>
          <xsl:attribute name="href">
            <xsl:value-of select="./url"/>
          </xsl:attribute>
          <xsl:value-of select="./boardName"/>
        </a>
      </td>
      <td>
        <a>
          <xsl:attribute name="href">
            <xsl:value-of select="./url"/>
          </xsl:attribute>
          <xsl:value-of select="./date"/>
        </a>
      </td>
      <td>
        <a>
          <xsl:attribute name="href">
            <xsl:value-of select="./url"/>
          </xsl:attribute>
          <xsl:value-of select="./source/name"/>
        </a>
      </td>
    </tr>
  </xsl:template>
</xsl:stylesheet>
