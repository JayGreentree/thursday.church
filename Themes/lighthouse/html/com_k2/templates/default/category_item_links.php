<?php

/**
 * @package		K2
 * @author		arrowthemes http://www.arrowthemes.com
 */

// no direct access
defined('_JEXEC') or die('Restricted access');

// Define default image size (do not change)
K2HelperUtilities::setDefaultImage($this->item, 'itemlist', $this->params);

?>

<li>
	<?php if($this->item->params->get('catItemTitle')): ?>
	  	<?php if ($this->item->params->get('catItemTitleLinked')): ?>
			<a href="<?php echo $this->item->link; ?>">
	  			<?php echo $this->item->title; ?>
	  		</a>
	  	<?php else: ?>
	  		<?php echo $this->item->title; ?>
	  	<?php endif; ?>
	<?php endif; ?>
</li>