# What does Obfuscar do? #

Basically, Obfuscar scrambles the metadata in a set of assemblies.  It renames everything to the minimal set of names that can be used to identify them, given signatures and type information.  Since these new names are shorter than the old ones, it also dramatically shrinks executable size.

## An Example ##

The following method is from the example included in the release:
```
       public ExampleUI( )
       {
               InitializeComponent( );

               ClassX cx = new ClassX( "Some Text" );

               displayText.Text = cx.DisplayText;
       }
```

The code can be decompiled (via Reflector) to:
```
       public ExampleUI()
       {
               this.InitializeComponent();
               this.displayText.Text = new ClassX("Some Text").get_DisplayText();
       }
```

After obfuscation, the code can be decompiled (via Reflector) to:
```
       public A()
       {
               this.A();
               this.a.Text = new A.A("Some Text").A();
       }
```

It's a simple example, but it scales...For example, given a reasonably sized code base, one could easily run into a class named 'A' (in the namespace 'A') with 7 methods, 4 properties, and 5 fields named 'A', with several more methods, properties, and fields named 'a'.

To try it out, see BasicExample.

## Caveat ##

It makes debugging / reverse engineering very difficult, but wouldn't stop someone who really wants to reverse engineer it.  It would at least slow them down, and would deter casual observers.
