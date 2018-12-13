using System;
using System.Reflection;

namespace Xamarin.Forms.Xaml
{
	internal sealed class MarkupExtensionParser : MarkupExpressionParser, IExpressionParser<object>
	{
		IMarkupExtension _markupExtension;

		public object Parse(string match, ref string remaining, IServiceProvider serviceProvider)
		{
			var typeResolver = serviceProvider.GetService(typeof (IXamlTypeResolver)) as IXamlTypeResolver;
			switch (match) {
			case "Binding":
				_markupExtension = new BindingExtension();
				break;
			case "TemplateBinding":
				_markupExtension = new TemplateBindingExtension();
				break;
			case "StaticResource":
				_markupExtension = new StaticResourceExtension();
				break;
			case "OnPlatform":
				_markupExtension = new OnPlatformExtension();
				break;
			case "OnIdiom":
				_markupExtension = new OnIdiomExtension();
				break;
			case "DataTemplate":
				_markupExtension = new DataTemplateExtension();
				break;
			default:
				if (typeResolver == null)
					return null;
				Type type;

				//The order of lookup is to look for the Extension-suffixed class name first and then look for the class name without the Extension suffix.
				if (!typeResolver.TryResolve(match + "Extension", out type) && !typeResolver.TryResolve(match, out type))
				{
					var lineInfoProvider = serviceProvider.GetService(typeof(IXmlLineInfoProvider)) as IXmlLineInfoProvider;
					var lineInfo = (lineInfoProvider != null) ? lineInfoProvider.XmlLineInfo : new XmlLineInfo();
					throw new XamlParseException(String.Format("MarkupExtension not found for {0}", match), lineInfo);
				}
				_markupExtension = Activator.CreateInstance(type) as IMarkupExtension;
				break;
			}

			if (_markupExtension == null)
			{
				var lineInfoProvider = serviceProvider.GetService(typeof (IXmlLineInfoProvider)) as IXmlLineInfoProvider;
				var lineInfo = (lineInfoProvider != null) ? lineInfoProvider.XmlLineInfo : new XmlLineInfo();
				throw new XamlParseException(String.Format("Missing public default constructor for MarkupExtension {0}", match),
					lineInfo);
			}

			char next;
			if (remaining == "}")
				return _markupExtension.ProvideValue(serviceProvider);

			string piece;
			while ((piece = GetNextPiece(ref remaining, out next)) != null)
				HandleProperty(piece, serviceProvider, ref remaining, next != '=');

			return _markupExtension.ProvideValue(serviceProvider);
		}

		protected override void SetPropertyValue(string prop, string strValue, object value, IServiceProvider serviceProvider)
		{
			MethodInfo setter;
			if (prop == null)
			{
				//implicit property
				var t = _markupExtension.GetType();
				prop = ApplyPropertiesVisitor.GetContentPropertyName(t.GetTypeInfo());
				if (prop == null)
					return;
				try {
					setter = t.GetRuntimeProperty(prop).SetMethod;
				} catch (AmbiguousMatchException e) {
					var lineInfo = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) is IXmlLineInfoProvider lineInfoProvider) ? lineInfoProvider.XmlLineInfo : new XmlLineInfo();
					throw new XamlParseException($"Multiple properties with name  '{t}.{prop}' found.", lineInfo, innerException: e);
				}
			}
			else {
				try
				{
					setter = _markupExtension.GetType().GetRuntimeProperty(prop).SetMethod;
				} catch (AmbiguousMatchException e) {
					var lineInfo = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) is IXmlLineInfoProvider lineInfoProvider) ? lineInfoProvider.XmlLineInfo : new XmlLineInfo();
					throw new XamlParseException($"Multiple properties with name  '{_markupExtension.GetType()}.{prop}' found.", lineInfo, innerException: e);
				}

			}
			if (value == null && strValue != null)
			{
				try {
					value = strValue.ConvertTo(_markupExtension.GetType().GetRuntimeProperty(prop).PropertyType,
						(Func<TypeConverter>)null, serviceProvider);
				} catch (AmbiguousMatchException e) {
					var lineInfo = (serviceProvider.GetService(typeof(IXmlLineInfoProvider)) is IXmlLineInfoProvider lineInfoProvider) ? lineInfoProvider.XmlLineInfo : new XmlLineInfo();
					throw new XamlParseException($"Multiple properties with name  '{_markupExtension.GetType()}.{prop}' found.", lineInfo, innerException: e);
				}
			}

			setter.Invoke(_markupExtension, new[] { value });
		}
	}
}