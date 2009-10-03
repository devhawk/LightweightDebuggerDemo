import clr
clr.AddReference("System.Xml")
from System.Xml import XmlDocument

def get_nodes(xml):
	return xml.SelectNodes("statuses/status/text")

def download_stuff():
	x = XmlDocument()

	x.Load("devhawk.xml")

	for n in get_nodes(x):
		txt = n.InnerText
		items.Add(txt)
		
download_stuff()