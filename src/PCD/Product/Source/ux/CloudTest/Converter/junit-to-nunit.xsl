<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xalan="https://xml.apache.org/xslt">
    <xsl:output method="xml" indent="yes" xalan:indent-amount="4" cdata-section-elements="message stack-trace"/>

    <xsl:template match="/">
         <xsl:variable name="didFail">
            <xsl:choose>
                <xsl:when test="count(//failure) > 0">Failure</xsl:when>
                <xsl:otherwise>Success</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <xsl:variable name="successValue">
            <xsl:choose>
                <xsl:when test="count(//failure) > 0">False</xsl:when>
                <xsl:otherwise>True</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <test-results name="{//testsuite[1]/@name}" total="{count(//testcase)}" errors="{count(//error)}" failures="{count(//failure)}" inconclusive="0" not-run="0" ignored="0" skipped="{count(//skipped)}" invalid="0" date ="2019-11-21" time="11:00:00">
            <environment nunit-version="2.6.0.12035" clr-version="2.0.50727.4963" os-version="Microsoft Windows NT 6.1.7600.0" platform="Win32NT" cwd="C:\Program Files\NUnit 2.6\bin" machine-name="Cloud-TestFAKE" user="cloudtest" user-domain="cloudtest-vm"/>
            <culture-info current-culture="en-US" current-uiculture="en-US"/>
            <test-suite type="TestFixture" name="On landing dashboard"  executed="True" result="{$didFail}" success="{$successValue}" time="7.128" asserts="0"  description="On landing dashboard">
                <results>
                    <xsl:for-each select="testsuites">
                        <xsl:apply-templates select="testcase"/>
                        <xsl:apply-templates select="testsuite"/>
                    </xsl:for-each>
                </results>
            </test-suite>
        </test-results>
    </xsl:template>
    <xsl:template match="testcase">
        <xsl:variable name="asserts">
            <xsl:choose>
                <xsl:when test="count(skipped) > 0">0</xsl:when>
                <xsl:when test="count(*) > 0">0</xsl:when>
                <xsl:when test="@assertions != ''">
                    <xsl:value-of select="@assertions"></xsl:value-of>
                </xsl:when>
                <xsl:otherwise>1</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>

        <xsl:variable name="time">
            <xsl:choose>
                <xsl:when test="count(skipped) > 0">0</xsl:when>
                <xsl:otherwise>
                    <xsl:value-of select="@time"></xsl:value-of>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:variable>

        <xsl:variable name="success">
            <xsl:choose>
                <xsl:when test="count(skipped) > 0">False</xsl:when>
                <xsl:when test="count(*) > 0">False</xsl:when>
                <xsl:otherwise>True</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>

        <xsl:variable name="executed">
            <xsl:choose>
                <xsl:when test="count(skipped) > 0">False</xsl:when>
                <xsl:otherwise>True</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>

        <xsl:variable name="result">
            <xsl:choose>
                <xsl:when test="count(skipped) > 0">Skipped</xsl:when>
                <xsl:when test="count(*) > 0">Failure</xsl:when>
                <xsl:otherwise>Success</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>

        <test-case name="{@classname} {@name}" description="{@classname}" success="{$success}" time="{$time}" executed="{$executed}" asserts="{$asserts}" result="{$result}">
            <xsl:apply-templates select="error"/>
            <xsl:apply-templates select="descendant::failure[1]"/>
            <xsl:apply-templates select="skipped"/>
        </test-case>
    </xsl:template>

    <xsl:template match="testsuite">
        <xsl:variable name="success">
            <xsl:choose>
                <xsl:when test="count(testcase/failure) > 0">False</xsl:when>
                <xsl:otherwise>True</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        
        <xsl:variable name="results">
            <xsl:choose>
                <xsl:when test="count(testcase/failure) > 0">Failure</xsl:when>
                <xsl:otherwise>Success</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        
        <xsl:variable name="asserts">
            <xsl:choose>
                <xsl:when test="@assertions != ''">
                    <xsl:value-of select="@assertions"></xsl:value-of>
                </xsl:when>
                <xsl:otherwise>
                    <xsl:value-of select="0"></xsl:value-of>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:variable>

        <test-suite type="TestFixture" name="{@name}" executed="True" result="{$results}" success="{$success}" time="{@time}" asserts="{$asserts}" description="{@file}">
            <xsl:if test="@file != ''">
                <categories>
                    <category name="{@file}" />
                </categories>
            </xsl:if>
            <results>
                <xsl:apply-templates select="testcase"/>
                <xsl:apply-templates select="testsuite"/>
            </results>
        </test-suite>
    </xsl:template>

    <xsl:template match="error">
        <xsl:variable name="message">
            <xsl:choose>
                <xsl:when test="@message != ''">
                    <xsl:value-of select="@message"></xsl:value-of>
                </xsl:when>
                <xsl:when test="@type != ''">
                    <xsl:value-of select="@type"></xsl:value-of>
                </xsl:when>
                <xsl:otherwise>No message</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>

        <xsl:variable name="stacktrace">
            <xsl:choose>
                <xsl:when test="text() != ''">
                    <xsl:value-of select="text()"></xsl:value-of>
                </xsl:when>
                <xsl:when test="@type != ''">
                    <xsl:value-of select="@type"></xsl:value-of>
                </xsl:when>
                <xsl:otherwise>No stack trace</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>

        <failure>
            <message>
                <xsl:value-of select="$message"></xsl:value-of>
            </message>
            <stack-trace>
                <xsl:value-of select="$stacktrace"></xsl:value-of>
            </stack-trace>
        </failure>
    </xsl:template>

    <xsl:template match="failure">
        <xsl:variable name="message">
            <xsl:choose>
                <xsl:when test="@message != ''">
                    <xsl:value-of select="@message"></xsl:value-of>
                </xsl:when>
                <xsl:when test="@type != ''">
                    <xsl:value-of select="@type"></xsl:value-of>
                </xsl:when>
                <xsl:otherwise>No message</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>

        <xsl:variable name="stacktrace">
            <xsl:choose>
                <xsl:when test="text() != ''">
                    <xsl:value-of select="text()"></xsl:value-of>
                </xsl:when>
                <xsl:when test="@type != ''">
                    <xsl:value-of select="@type"></xsl:value-of>
                </xsl:when>
                <xsl:otherwise>No stack trace</xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <failure>
            <message>
                <xsl:value-of select="$message"></xsl:value-of>
            </message>
            <stack-trace>
                <xsl:value-of select="$stacktrace"></xsl:value-of>
            </stack-trace>
        </failure>
    </xsl:template>

    <xsl:template match="skipped">
        <reason>
            <message>Skipped</message>
        </reason>
    </xsl:template>
</xsl:stylesheet>
