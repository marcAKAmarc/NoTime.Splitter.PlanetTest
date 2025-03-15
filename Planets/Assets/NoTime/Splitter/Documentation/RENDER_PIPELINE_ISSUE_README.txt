The splitter sample scenes were created with the Built-in render pipepline.

If you are not using Built-in render pipeline and material conversion did not automatically happen (pink scene), please try the following:

-HDRP-
1.  Backup your project if needed.
2.  Navigate to Assets > NoTime > Splitter > Demo > Materials and select all materials. 
3.  Window -> Rendering -> HDRP Wizard
4.  Click 'Convert Selected Built-in Materials to HDRP'

-URP-
URP does not automatically convert materials on some versions of unity.
In Unity 2021.2 and newer:
1.  Back up your project if needed.
2.  At the top, select Window -> Rendering -> Render Pipeline Converter.
3.  Open the drop-down and select 'Built-in to URP'. 
4.  Select 'Material Upgrade' and click 'Initialize And Convert'

In Unity 2021.2 and older:
1.  Backup your project if needed.
2.  Edit > Rendering > Materials > Convert All Built-In Materials to URP
 