using System;
using System.Collections.Generic;
using System.Text;

namespace PriceLens;

public class Angebot
{
    public decimal preis;
    public string waehrung ="";

    public Produkt? produkt;
    public Shop? shop;
}
