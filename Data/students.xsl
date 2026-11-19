<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="UTF-8" indent="yes"/>

  <xsl:template match="/university">
    <html>
      <head>
        <meta charset="utf-8"/>
        <title>Успішність студентів</title>
        <style>
          body { font-family: Segoe UI, Arial, sans-serif; margin: 20px; color: #222; background: #f5f6fa; }
          h1 { font-size: 22px; margin: 0 0 8px 0; }
          .meta { font-size: 13px; color: #555; margin-bottom: 16px; line-height: 1.5; }
          .meta b { color: #222; }
          .card { background: #fff; border: 1px solid #d9dce6; border-radius: 8px; padding: 14px 16px; margin-bottom: 14px; }
          .card h2 { font-size: 17px; margin: 0 0 8px 0; color: #1f3a93; }
          .tag { display: inline-block; background: #eef1fb; border-radius: 12px; padding: 2px 10px; margin: 2px 4px 2px 0; font-size: 12px; }
          table { border-collapse: collapse; width: 100%; margin-top: 10px; font-size: 13px; }
          th { background: #1f3a93; color: #fff; text-align: left; padding: 6px 8px; }
          td { border-bottom: 1px solid #e3e5ee; padding: 5px 8px; }
          .g-a { color: #1b7f3b; font-weight: bold; }
          .g-b { color: #2b6cb0; font-weight: bold; }
          .g-c { color: #b7791f; font-weight: bold; }
          .g-f { color: #c53030; font-weight: bold; }
          .avg { margin-top: 8px; font-size: 13px; }
          .empty { padding: 18px; background: #fff3cd; border-radius: 8px; }
        </style>
      </head>
      <body>
        <h1>Успішність студентів</h1>
        <div class="meta">
          <xsl:if test="@strategy">
            Спосіб аналізу: <b><xsl:value-of select="@strategy"/></b><br/>
          </xsl:if>
          <xsl:if test="@query">
            Запит: <b><xsl:value-of select="@query"/></b><br/>
          </xsl:if>
          Знайдено студентів: <b><xsl:value-of select="count(student)"/></b>
          <xsl:if test="count(student/discipline/@grade) &gt; 0">
            ; середній бал за вибіркою:
            <b><xsl:value-of select="format-number(sum(student/discipline/@grade) div count(student/discipline/@grade), '0.0')"/></b>
          </xsl:if>
        </div>
        <xsl:choose>
          <xsl:when test="count(student) = 0">
            <div class="empty">За заданими параметрами пошуку нічого не знайдено.</div>
          </xsl:when>
          <xsl:otherwise>
            <xsl:apply-templates select="student">
              <xsl:sort select="@PIB" data-type="text" lang="uk"/>
            </xsl:apply-templates>
          </xsl:otherwise>
        </xsl:choose>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="student">
    <div class="card">
      <h2><xsl:value-of select="position()"/>. <xsl:value-of select="@PIB"/></h2>
      <div>
        <xsl:for-each select="@*[name() != 'PIB']">
          <span class="tag">
            <b><xsl:call-template name="label"><xsl:with-param name="n" select="name()"/></xsl:call-template>:</b>
            <xsl:text> </xsl:text><xsl:value-of select="."/>
          </span>
        </xsl:for-each>
      </div>
      <xsl:if test="discipline">
        <table>
          <tr>
            <th>Дисципліна</th><th>Семестр</th><th>Кредити</th><th>Контроль</th><th>Викладач</th><th>Оцінка</th>
          </tr>
          <xsl:for-each select="discipline">
            <xsl:sort select="@semester" data-type="number"/>
            <tr>
              <td><xsl:call-template name="cell"><xsl:with-param name="v" select="@name"/></xsl:call-template></td>
              <td><xsl:call-template name="cell"><xsl:with-param name="v" select="@semester"/></xsl:call-template></td>
              <td><xsl:call-template name="cell"><xsl:with-param name="v" select="@credits"/></xsl:call-template></td>
              <td><xsl:call-template name="cell"><xsl:with-param name="v" select="@control"/></xsl:call-template></td>
              <td><xsl:call-template name="cell"><xsl:with-param name="v" select="@teacher"/></xsl:call-template></td>
              <td>
                <xsl:choose>
                  <xsl:when test="not(@grade)">—</xsl:when>
                  <xsl:otherwise>
                    <span>
                      <xsl:attribute name="class">
                        <xsl:choose>
                          <xsl:when test="@grade &gt;= 90">g-a</xsl:when>
                          <xsl:when test="@grade &gt;= 75">g-b</xsl:when>
                          <xsl:when test="@grade &gt;= 60">g-c</xsl:when>
                          <xsl:otherwise>g-f</xsl:otherwise>
                        </xsl:choose>
                      </xsl:attribute>
                      <xsl:value-of select="@grade"/>
                    </span>
                  </xsl:otherwise>
                </xsl:choose>
              </td>
            </tr>
          </xsl:for-each>
        </table>
        <xsl:if test="count(discipline/@grade) &gt; 0">
          <div class="avg">Середній бал студента:
            <b><xsl:value-of select="format-number(sum(discipline/@grade) div count(discipline/@grade), '0.0')"/></b>
          </div>
        </xsl:if>
      </xsl:if>
    </div>
  </xsl:template>

  <xsl:template name="cell">
    <xsl:param name="v"/>
    <xsl:choose>
      <xsl:when test="string($v) != ''"><xsl:value-of select="$v"/></xsl:when>
      <xsl:otherwise>—</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="label">
    <xsl:param name="n"/>
    <xsl:choose>
      <xsl:when test="$n = 'ID'">Код</xsl:when>
      <xsl:when test="$n = 'faculty'">Факультет</xsl:when>
      <xsl:when test="$n = 'department'">Кафедра</xsl:when>
      <xsl:when test="$n = 'course'">Курс</xsl:when>
      <xsl:when test="$n = 'group'">Група</xsl:when>
      <xsl:when test="$n = 'form'">Форма навчання</xsl:when>
      <xsl:when test="$n = 'scholarship'">Стипендія</xsl:when>
      <xsl:when test="$n = 'email'">E-mail</xsl:when>
      <xsl:when test="$n = 'phone'">Телефон</xsl:when>
      <xsl:otherwise><xsl:value-of select="$n"/></xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>
