<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="text" encoding="utf-8" omit-xml-declaration="yes" indent="no" />
	
	<xsl:variable name="newline">
<xsl:text>
</xsl:text>
	</xsl:variable>

	<xsl:variable name="quote">"</xsl:variable>

	<xsl:variable name="base_url">http://synchronicity.sourceforge.net/</xsl:variable>
	<xsl:variable name="image_path">D:/Documents/Sites Web/Sourceforge/Synchronicity/</xsl:variable>

	<xsl:template match="//a[@href and not(img)]">\href{<xsl:value-of select="@href"/>}{<xsl:apply-templates />}</xsl:template>
	<xsl:template match="//a[@href and not(img or starts-with(@href, 'http') or starts-with(@href, 'mailto'))]">\href{<xsl:value-of select="$base_url"/><xsl:value-of select="@href"/>}{<xsl:apply-templates />}</xsl:template>
	
	<xsl:template match="//br">~\\<xsl:apply-templates /></xsl:template>
	<xsl:template match="//em">\emph{<xsl:apply-templates />}</xsl:template>
	
	<xsl:template match="//h1">\begin{center}\Huge <xsl:apply-templates />\end{center}\tableofcontents{}</xsl:template>
	<xsl:template match="//h2">\section{<xsl:apply-templates />}</xsl:template>
	<xsl:template match="//h3">\subsection{<xsl:apply-templates />}</xsl:template>
	<xsl:template match="//h4">\subsubsection{<xsl:apply-templates />}</xsl:template>
	<xsl:template match="//h5">\paragraph{<xsl:apply-templates />}</xsl:template>
	
	<xsl:template match="//a[@href]/img">\includeimage{<xsl:value-of select="$image_path"/><xsl:value-of select="../@href"/>}{<xsl:value-of select="@alt"/>}</xsl:template> <!-- Include full size image, not thumbnail. -->
	<xsl:template match="//*[not(self::a)]/img">\includeimage{<xsl:value-of select="$image_path"/><xsl:value-of select="@src"/>}{<xsl:value-of select="@alt"/>}<xsl:apply-templates /></xsl:template>
	<xsl:template match="//img[contains(@class, 'latex-silent')]">\includeimage{<xsl:value-of select="$image_path"/><xsl:value-of select="@src"/>}{}<xsl:apply-templates /></xsl:template> <!-- Don't include a label -->
	
	<xsl:template match="//li">\item <xsl:apply-templates /></xsl:template>
	
	<xsl:template match="//p"><xsl:apply-templates /><xsl:value-of select="$newline"/></xsl:template>
	<xsl:template match="//samp">\lstinline!<xsl:apply-templates />!</xsl:template>
	<xsl:template match="//sup">\textsuperscript{<xsl:apply-templates />}</xsl:template>
	
	<xsl:template match="//table">\begin{tabular}{<xsl:value-of select="@summary"/>}<xsl:apply-templates />\end{tabular}</xsl:template>
	<xsl:template match="//td"><xsl:value-of select="@title"/><xsl:apply-templates /></xsl:template>
	<xsl:template match="//th"><xsl:value-of select="@title"/>\textbf{<xsl:apply-templates />}</xsl:template>
	<xsl:template match="//tr"><xsl:apply-templates />\\</xsl:template>
	
	<xsl:template match="//ol">\begin{enumerate}<xsl:apply-templates />\end{enumerate}</xsl:template>
	<xsl:template match="//ul">\begin{itemize}<xsl:apply-templates />\end{itemize}</xsl:template>

	<xsl:template match="//*[not(self::samp)]/text()"><xsl:call-template name="replace-left-quote"><xsl:with-param name="text" select="."/></xsl:call-template></xsl:template>
	
	<!--<xsl:template match="//div"></xsl:template>-->
	<xsl:template match="//*[contains(@class, 'latex-discard')]"></xsl:template>
	
	<xsl:template name="replace-left-quote">
		<xsl:param name="text"/>
		<xsl:choose>
			<xsl:when test="contains($text,$quote)">
				<xsl:value-of select="substring-before($text,$quote)"/>``<xsl:call-template name="replace-right-quote"><xsl:with-param name="text" select="substring-after($text,$quote)"/></xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$text"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="replace-right-quote">
		<xsl:param name="text"/>
		<xsl:choose>
			<xsl:when test="contains($text,$quote)">
				<xsl:value-of select="substring-before($text,$quote)"/>''<xsl:call-template name="replace-left-quote"><xsl:with-param name="text" select="substring-after($text,$quote)"/></xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$text"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template match="/">
		\documentclass{article}

		\usepackage[T1]{fontenc}
		\usepackage[utf8]{inputenc}
		\usepackage{lmodern}
		\usepackage{listings}
		\usepackage[english]{babel}
		\usepackage{fullpage, wrapfig, grffile, graphicx, ifthen, float, hyperref}

		\hypersetup{
			bookmarks=true,																% show bookmarks bar?
			unicode=false,																% non-Latin characters in Acrobat’s bookmarks
			pdftoolbar=true,															% show Acrobat’s toolbar?
			pdfmenubar=true,															% show Acrobat’s menu?
			pdffitwindow=false,															% window fit to page when opened
			pdfstartview={FitH},														% fits the width of the page to the window
			pdftitle={Create Synchronicity User Manual},														% title
			pdfauthor={Create Software (Clément Pit--Claudel)},							% author
			pdfsubject={Create Synchronicity User Manual},								% subject of the document
			pdfcreator={Create Software's Xhtml2Latex},									% creator of the document
			pdfproducer={Create Software (Clément Pit--Claudel)},						% producer of the document
			pdfkeywords={manual} {backup} {synchronization} {create} {synchronicity},	% list of keywords
			pdfnewwindow=true,															% links in new window
			colorlinks=true,															% false: boxed links; true: colored links
			linkcolor=red,																% color of internal links
			citecolor=green,															% color of links to bibliography
			filecolor=magenta,															% color of file links
			urlcolor=cyan																% color of external links
		}

		\newlength{\initialWidth}
		\newlength{\maxPicWidth}
		\setlength{\maxPicWidth}{0.4\textwidth} %TODO: Change me if you want larger images. [[scalefactor]]
		\newcommand{\fitpic}[1]{%
			\settowidth{\initialWidth}{\includegraphics{#1}}
			\message{#1 Image width: \the\initialWidth, Page width: \the\maxPicWidth}

			\ifthenelse{\lengthtest{\initialWidth > \maxPicWidth}}
				{\noindent\includegraphics[width=\maxPicWidth]{#1}}
				{\centering\includegraphics{#1}\par}
		}
		\newcommand{\includeimage}[2]{
			\ifthenelse{\equal{#2}{}}
				{
					\begin{wrapfigure}[3]{l}{0pt}
						\raisebox{15pt}[\height]{
							\fitpic{#1}
						}
					\end{wrapfigure}
				} %FIXME: [3]
				{
					\begin{figure}[H]
						\begin{center}
							\fitpic{#1}
							\caption{#2}
						\end{center}
					\end{figure}
				}
		}

		\tolerance=10000
		\linespread{1.2}
		\restylefloat{figure} %Enables the H placement modifier.

		\begin{document}
			<xsl:apply-templates />
		\end{document}
	</xsl:template>
</xsl:stylesheet>